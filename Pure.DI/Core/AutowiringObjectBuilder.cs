namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
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
        private readonly ITracer _tracer;

        public AutowiringObjectBuilder(
            IDiagnostic diagnostic,
            IBuildContext buildContext, 
            IConstructorsResolver constructorsResolver,
            IAttributesService attributesService,
            ITracer tracer)
        {
            _diagnostic = diagnostic;
            _buildContext = buildContext;
            _constructorsResolver = constructorsResolver;
            _attributesService = attributesService;
            _tracer = tracer;
        }

        [SuppressMessage("ReSharper", "InvertIf")]
        public ExpressionSyntax? TryBuild(IBuildStrategy buildStrategy, Dependency dependency)
        {
            var objectCreationExpression = (
                from ctor in _constructorsResolver.Resolve(dependency)
                let parameters = ResolveMethodParameters(_buildContext.TypeResolver, buildStrategy, dependency, ctor, true).ToList()
                where parameters.All(i => i != null)
                let arguments = SyntaxFactory.SeparatedList(
                    from parameter in parameters
                    select SyntaxFactory.Argument(parameter))
                select CreateObject(ctor, dependency, arguments)
            ).FirstOrDefault();
            
            if (Equals(objectCreationExpression, default))
            {
                _tracer.Save();
                return null;
            }

            var members = (
                from member in dependency.Implementation.Type.GetMembers()
                where member.MetadataName != ".ctor"
                from expression in _attributesService.GetAttributeArgumentExpressions(AttributeKind.Order, member)
                let order = ((expression as LiteralExpressionSyntax)?.Token)?.Value as IComparable
                where order != null
                orderby order 
                select member)
                .Distinct(SymbolEqualityComparer.Default)
                .ToList();

            if (members.Any())
            {
                List<StatementSyntax> factoryStatements = new();
                foreach (var member in members)
                {
                    if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public && member.DeclaredAccessibility != Accessibility.Internal)
                    {
                        var error = $"{member} is inaccessible in {dependency.Implementation}.";
                        _diagnostic.Error(Diagnostics.Error.MemberIsInaccessible, error, member.Locations.FirstOrDefault());
                        throw new HandledException(error);
                    }

                    switch (member)
                    {
                        case IMethodSymbol method:
                            var arguments =
                                from parameter in ResolveMethodParameters(_buildContext.TypeResolver, buildStrategy, dependency, method, false)
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
                                var fieldValue = ResolveInstance(filed, dependency, filed.Type, _buildContext.TypeResolver, buildStrategy, filed.Locations, false);
                                var assignmentExpression = SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(InstanceVarName),
                                        SyntaxFactory.Token(SyntaxKind.DotToken),
                                        SyntaxFactory.IdentifierName(filed.Name)),
                                    fieldValue!);

                                factoryStatements.Add(SyntaxFactory.ExpressionStatement(assignmentExpression));
                            }

                            break;

                        case IPropertySymbol property:
                            if (property.SetMethod != null && !property.IsReadOnly && !property.IsStatic)
                            {
                                var propertyValue = ResolveInstance(property, dependency, property.Type, _buildContext.TypeResolver, buildStrategy, property.Locations, false);
                                var assignmentExpression = SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(InstanceVarName),
                                        SyntaxFactory.Token(SyntaxKind.DotToken),
                                        SyntaxFactory.IdentifierName(property.Name)),
                                    propertyValue!);

                                factoryStatements.Add(SyntaxFactory.ExpressionStatement(assignmentExpression));
                            }

                            break;
                    }
                }

                if (factoryStatements.Any())
                {
                    var memberKey = new MemberKey($"Create{dependency.Binding.Implementation}", dependency);
                    var factoryName = _buildContext.NameService.FindName(memberKey);
                    var type = dependency.Implementation.TypeSyntax;
                    var factoryMethod = SyntaxFactory.MethodDeclaration(type, SyntaxFactory.Identifier(factoryName))
                        .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
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
                    return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(factoryName))
                        .WithCommentBefore($"// {dependency.Binding}");
                }
            }

            return objectCreationExpression
                .WithCommentBefore($"// {dependency.Binding}");
        }

        private ExpressionSyntax? ResolveInstance(
            ISymbol target,
            Dependency targetDependency,
            ITypeSymbol defaultType,
            ITypeResolver typeResolver,
            IBuildStrategy buildStrategy,
            ImmutableArray<Location> resolveLocations,
            bool probe)
        {
            LiteralExpressionSyntax? defaultValue = null;
            if (target is IParameterSymbol {HasExplicitDefaultValue: true} parameter)
            {
                defaultValue = parameter.ExplicitDefaultValue.ToLiteralExpression();
            }

            var resolvingType = GetDependencyType(target, targetDependency.Implementation) ?? new SemanticType(defaultType, targetDependency.Implementation);
            var tag = (ExpressionSyntax?) _attributesService.GetAttributeArgumentExpressions(AttributeKind.Tag, target).FirstOrDefault();
            var dependency = typeResolver.Resolve(resolvingType, tag);
            if (!dependency.IsResolved && defaultValue != null)
            {
                return defaultValue;
            }
            
            if (buildStrategy.TryBuild(dependency, resolvingType) == null)
            {
                return null;
            }

            switch (dependency.Implementation.Type)
            {
                case INamedTypeSymbol namedType:
                {
                    var type = new SemanticType(namedType, targetDependency.Implementation);
                    var constructedType = dependency.TypesMap.ConstructType(type);
                    if (!dependency.Implementation.Equals(constructedType))
                    {
                        _buildContext.AddBinding(new BindingMetadata(dependency.Binding, constructedType, null));
                    }

                    if (dependency.IsResolved)
                    {
                        return buildStrategy.TryBuild(dependency, resolvingType);
                    }

                    if (probe)
                    {
                        return null;
                    }

                    var dependencyType = dependency.Implementation.TypeSyntax;
                    return SyntaxFactory.CastExpression(dependencyType,
                        SyntaxFactory.InvocationExpression(SyntaxFactory.ParseName(nameof(IContext.Resolve)))
                            .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(dependencyType))));
                }

                case IArrayTypeSymbol arrayType:
                {
                    var type = new SemanticType(arrayType, targetDependency.Implementation);
                    var arrayTypeDescription = typeResolver.Resolve(type, null);
                    if (arrayTypeDescription.IsResolved)
                    {
                        return buildStrategy.TryBuild(arrayTypeDescription, type);
                    }

                    break;
                }
            }
            
            var error = $"Unsupported type {dependency.Implementation}.";
            _diagnostic.Error(Diagnostics.Error.Unsupported, error, resolveLocations.FirstOrDefault());
            throw new HandledException(error);
        }

        private IEnumerable<ExpressionSyntax?> ResolveMethodParameters(ITypeResolver typeResolver, IBuildStrategy buildStrategy, Dependency dependency, IMethodSymbol method, bool probe) =>
            from parameter in method.Parameters
            select ResolveInstance(parameter, dependency, parameter.Type, typeResolver, buildStrategy, parameter.Locations, probe);

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
                _diagnostic.Warning(Diagnostics.Warning.CtorIsObsoleted, $"The constructor {ctor} marked as obsoleted is used.", ctor.Locations.FirstOrDefault());
            }

            return SyntaxFactory.ObjectCreationExpression(typeSyntax)
                .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        }
    }
}