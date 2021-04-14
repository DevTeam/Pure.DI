namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class AutowiringObjectBuilder : IObjectBuilder
    {
        private const string InstanceVarName = "instance";
        private readonly IDiagnostic _diagnostic;
        private readonly IBuildContext _buildContext;
        private readonly IConstructorsResolver _constructorsResolver;
        private readonly IAttributesService _attributesService;

        public AutowiringObjectBuilder(
            IDiagnostic diagnostic,
            IBuildContext buildContext, 
            IConstructorsResolver constructorsResolver,
            IAttributesService attributesService)
        {
            _diagnostic = diagnostic;
            _buildContext = buildContext;
            _constructorsResolver = constructorsResolver;
            _attributesService = attributesService;
        }

        [SuppressMessage("ReSharper", "InvertIf")]
        public ExpressionSyntax Build(IBuildStrategy buildStrategy, Dependency dependency)
        {
            var objectCreationExpression = (
                from ctor in _constructorsResolver.Resolve(dependency)
                let parameters = ResolveMethodParameters(_buildContext.TypeResolver, buildStrategy, dependency, ctor)
                where parameters.All(i => i != null)
                let arguments = SyntaxFactory.SeparatedList(
                    from parameter in parameters
                    select SyntaxFactory.Argument(parameter))
                select CreateObject(ctor, dependency, arguments)
            ).FirstOrDefault();

            if (Equals(objectCreationExpression, default))
            {
                var message = $"Cannot find an accessible constructor for {dependency}.";
                _diagnostic.Error(Diagnostics.CannotFindCtor, message, dependency.Binding.Location);
                return SyntaxFactory.ParseName(message);
            }

            var members = (
                from member in dependency.Implementation.Type.GetMembers()
                where member.MetadataName != ".ctor"
                from expression in _attributesService.GetAttributeArgumentExpressions(AttributeKind.Order, member)
                let order = ((expression as LiteralExpressionSyntax)?.Token)?.Value as IComparable
                where order != null
                orderby order 
                select member)
                .Distinct()
                .ToList();

            if (members.Any())
            {
                List<StatementSyntax> factoryStatements = new();
                foreach (var member in members)
                {
                    if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public && member.DeclaredAccessibility != Accessibility.Internal)
                    {
                        _diagnostic.Error(Diagnostics.MemberIsInaccessible, $"{member} is inaccessible in {dependency.Implementation}.", member.Locations.FirstOrDefault());
                        continue;
                    }

                    switch (member)
                    {
                        case IMethodSymbol method:
                            var arguments =
                                from parameter in ResolveMethodParameters(_buildContext.TypeResolver, buildStrategy, dependency, method)
                                select SyntaxFactory.Argument(parameter);

                            var call = SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.ParseName(InstanceVarName),
                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                    SyntaxFactory.IdentifierName(method.Name)))
                                .AddArgumentListArguments(arguments.ToArray());

                            factoryStatements.Add(SyntaxFactory.ExpressionStatement(call));
                            break;

                        case IFieldSymbol filed:
                            if (!filed.IsReadOnly && !filed.IsStatic && !filed.IsConst)
                            {
                                var fieldValue = ResolveInstance(filed, dependency, filed.Type, _buildContext.TypeResolver, buildStrategy);
                                var assignmentExpression = SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(InstanceVarName),
                                        SyntaxFactory.Token(SyntaxKind.DotToken),
                                        SyntaxFactory.IdentifierName(filed.Name)),
                                    fieldValue);

                                factoryStatements.Add(SyntaxFactory.ExpressionStatement(assignmentExpression));
                            }

                            break;

                        case IPropertySymbol property:
                            if (property.SetMethod != null && !property.IsReadOnly && !property.IsStatic)
                            {
                                var propertyValue = ResolveInstance(property, dependency, property.Type, _buildContext.TypeResolver, buildStrategy);
                                var assignmentExpression = SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(InstanceVarName),
                                        SyntaxFactory.Token(SyntaxKind.DotToken),
                                        SyntaxFactory.IdentifierName(property.Name)),
                                    propertyValue);

                                factoryStatements.Add(SyntaxFactory.ExpressionStatement(assignmentExpression));
                            }

                            break;
                    }
                }

                if (factoryStatements.Any())
                {
                    var memberKey = new MemberKey($"Create{dependency.Implementation.Type.Name}", dependency.Implementation, dependency.Tag);
                    var factoryName = _buildContext.NameService.FindName(memberKey);
                    var type = dependency.Implementation.TypeSyntax;
                    var factoryMethod = SyntaxFactory.MethodDeclaration(type, SyntaxFactory.Identifier(factoryName))
                        .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveOptimizationAndInliningAttr))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                        .AddBodyStatements(
                            SyntaxFactory.LocalDeclarationStatement(
                                SyntaxFactory.VariableDeclaration(type)
                                    .AddVariables(
                                        SyntaxFactory.VariableDeclarator(InstanceVarName)
                                            .WithInitializer(SyntaxFactory.EqualsValueClause(objectCreationExpression)))))
                        .AddBodyStatements(factoryStatements.ToArray())
                        .AddBodyStatements(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.IdentifierName(InstanceVarName)));

                    _buildContext.GetOrAddMember(memberKey, () => factoryMethod);
                    return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(factoryName));
                }
            }

            return objectCreationExpression;
        }

        private ExpressionSyntax ResolveInstance(
            ISymbol target,
            Dependency targetDependency,
            ITypeSymbol defaultType,
            ITypeResolver typeResolver,
            IBuildStrategy buildStrategy)
        {
            var type = GetDependencyType(target, targetDependency.Implementation) ?? new SemanticType(defaultType, targetDependency.Implementation);
            var tag = (ExpressionSyntax?) _attributesService.GetAttributeArgumentExpressions(AttributeKind.Tag, target).FirstOrDefault();
            var typeDescription = typeResolver.Resolve(type, tag);
            switch (typeDescription.Implementation.Type)
            {
                case INamedTypeSymbol namedType:
                {
                    var constructedType = typeDescription.TypesMap.ConstructType(new SemanticType(namedType, targetDependency.Implementation));
                    if (!typeDescription.Implementation.Equals(constructedType))
                    {
                        _buildContext.AddBinding(new BindingMetadata(typeDescription.Binding, constructedType));
                    }

                    if (typeDescription.IsResolved)
                    {
                        return buildStrategy.Build(_buildContext.TypeResolver.Resolve(constructedType, typeDescription.Tag));
                    }

                    var dependencyType = typeDescription.Implementation.TypeSyntax;
                    return SyntaxFactory.CastExpression(dependencyType,
                        SyntaxFactory.InvocationExpression(SyntaxFactory.ParseName(nameof(IContext.Resolve)))
                            .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(dependencyType))));
                }

                case IArrayTypeSymbol arrayType:
                {
                    var arrayTypeDescription = typeResolver.Resolve(new SemanticType(arrayType, targetDependency.Implementation), null);
                    if (arrayTypeDescription.IsResolved)
                    {
                        return buildStrategy.Build(arrayTypeDescription);
                    }

                    break;
                }
            }

            _diagnostic.Error(Diagnostics.Unsupported, $"Unsupported type {typeDescription.Implementation}.", target.Locations.FirstOrDefault());
            throw Diagnostics.ErrorShouldTrowException;
        }

        private IEnumerable<ExpressionSyntax?> ResolveMethodParameters(ITypeResolver typeResolver, IBuildStrategy buildStrategy, Dependency dependency, IMethodSymbol method) =>
            from parameter in method.Parameters
            select ResolveInstance(parameter, dependency, parameter.Type, typeResolver, buildStrategy);

        private SemanticType? GetDependencyType(ISymbol type, SemanticModel semanticModel) =>
        (
            from expression in _attributesService.GetAttributeArgumentExpressions(AttributeKind.Type, type)
            let typeExpression = (expression as TypeOfExpressionSyntax)?.Type
            where typeExpression != null
            let typeSemanticModel = typeExpression.GetSemanticModel(semanticModel)
            let typeSymbol = typeSemanticModel.GetTypeInfo(typeExpression).Type
            where typeSymbol != null
            select new SemanticType(typeSymbol, typeSemanticModel)).FirstOrDefault();

        // ReSharper disable once SuggestBaseTypeForParameter
        private ExpressionSyntax CreateObject(IMethodSymbol ctor, Dependency dependency, SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            var typeSyntax = dependency.Implementation.TypeSyntax;
            if (typeSyntax.IsKind(SyntaxKind.TupleType))
            {
                return SyntaxFactory.TupleExpression()
                    .WithArguments(arguments);
            }

            if (ctor.GetAttributes(typeof(ObsoleteAttribute), dependency.Implementation.SemanticModel).Any())
            {
                _diagnostic.Warning(Diagnostics.CtorIsObsoleted, $"Constructor {ctor} is obsoleted.", dependency.Binding.Location);
            }

            return SyntaxFactory.ObjectCreationExpression(typeSyntax)
                .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        }
    }
}