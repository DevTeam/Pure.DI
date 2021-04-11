namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
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

        public ExpressionSyntax TryBuild(IBindingExpressionStrategy bindingExpressionStrategy, TypeDescription typeDescription)
        {
            var ctorInfo = (
                from ctor in _constructorsResolver.Resolve(typeDescription)
                let parameters = ResolveMethodParameters(_buildContext.TypeResolver, bindingExpressionStrategy, typeDescription, ctor)
                where parameters.All(i => i != null)
                let arguments = SyntaxFactory.SeparatedList(
                    from parameter in parameters
                    select SyntaxFactory.Argument(parameter))
                let objectCreationExpression = CreateObject(ctor, typeDescription, arguments)
                select (typeDescription, objectCreationExpression)
            ).FirstOrDefault();

            if (Equals(ctorInfo.objectCreationExpression, default))
            {
                var message = $"Cannot find an accessible constructor for {typeDescription}.";
                _diagnostic.Error(Diagnostics.CannotFindCtor, message, typeDescription.Binding.Location);
                return SyntaxFactory.ParseName(message);
            }

            var members = (
                from member in ctorInfo.typeDescription.Type.GetMembers()
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
                    if (member.IsStatic || (member.DeclaredAccessibility != Accessibility.Public && member.DeclaredAccessibility != Accessibility.Internal))
                    {
                        _diagnostic.Error(Diagnostics.MemberIsInaccessible, $"{member} is inaccessible in {ctorInfo.typeDescription.Type}.", member.Locations.FirstOrDefault());
                        continue;
                    }

                    switch (member)
                    {
                        case IMethodSymbol method:
                            var arguments =
                                from parameter in ResolveMethodParameters(_buildContext.TypeResolver, bindingExpressionStrategy, typeDescription, method)
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
                                var fieldValue = ResolveInstance(filed, typeDescription, filed.Type, _buildContext.TypeResolver, bindingExpressionStrategy);
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
                                var propertyValue = ResolveInstance(property, typeDescription, property.Type, _buildContext.TypeResolver, bindingExpressionStrategy);
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
                    var memberKey = new MemberKey($"Create{typeDescription.Type.Name}", typeDescription.Type, typeDescription.Tag);
                    var factoryName = _buildContext.NameService.FindName(memberKey);
                    var type = typeDescription.Type.ToTypeSyntax(typeDescription.SemanticModel);
                    var factoryMethod = SyntaxFactory.MethodDeclaration(type, SyntaxFactory.Identifier(factoryName))
                        .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveOptimizationAndInliningAttr))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                        .AddBodyStatements(
                            SyntaxFactory.LocalDeclarationStatement(
                                SyntaxFactory.VariableDeclaration(type)
                                    .AddVariables(
                                        SyntaxFactory.VariableDeclarator(InstanceVarName)
                                            .WithInitializer(SyntaxFactory.EqualsValueClause(ctorInfo.objectCreationExpression)))))
                        .AddBodyStatements(factoryStatements.ToArray())
                        .AddBodyStatements(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.IdentifierName(InstanceVarName)));

                    _buildContext.GetOrAddMember(memberKey, () => factoryMethod);
                    return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(factoryName));
                }
            }

            return ctorInfo.objectCreationExpression;
        }

        private ExpressionSyntax ResolveInstance(
            ISymbol target,
            TypeDescription targetTypeDescription,
            ITypeSymbol defaultType,
            ITypeResolver typeResolver,
            IBindingExpressionStrategy bindingExpressionStrategy)
        {
            var type = GetContractType(target, targetTypeDescription.SemanticModel) ?? defaultType;
            var tag = (ExpressionSyntax?) _attributesService.GetAttributeArgumentExpressions(AttributeKind.Tag, target).FirstOrDefault();
            var typeDescription = typeResolver.Resolve(type, tag);
            if (typeDescription.Type is INamedTypeSymbol namedType)
            {
                var constructedType = typeDescription.TypesMap.ConstructType(namedType);
                if (!typeDescription.Type.Equals(constructedType, SymbolEqualityComparer.Default))
                {
                    _buildContext.AddBinding(new BindingMetadata(typeDescription.Binding, constructedType));
                }

                if (typeDescription.IsResolved)
                {
                    return bindingExpressionStrategy.TryBuild(constructedType, typeDescription.Tag);
                }

                var contractType = typeDescription.Type.ToTypeSyntax(typeDescription.SemanticModel);
                return SyntaxFactory.CastExpression(contractType,
                    SyntaxFactory.InvocationExpression(SyntaxFactory.ParseName(nameof(IContext.Resolve)))
                        .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(contractType))));
            }

            if (typeDescription.Type is IArrayTypeSymbol arrayType)
            {
                var arrayTypeDescription = typeResolver.Resolve(arrayType, null);
                if (arrayTypeDescription.IsResolved)
                {
                    return bindingExpressionStrategy.TryBuild(arrayTypeDescription);
                }
            }

            _diagnostic.Error(Diagnostics.Unsupported, $"Unsupported type {typeDescription.Type}.", target.Locations.FirstOrDefault());
            throw new InvalidOperationException("Diagnostic.Error should throw an exception.");
        }

        private IEnumerable<ExpressionSyntax?> ResolveMethodParameters(ITypeResolver typeResolver, IBindingExpressionStrategy bindingExpressionStrategy, TypeDescription typeDescription, IMethodSymbol method) =>
            from parameter in method.Parameters
            select ResolveInstance(parameter, typeDescription, parameter.Type, typeResolver, bindingExpressionStrategy);

        private ITypeSymbol? GetContractType(ISymbol type, SemanticModel semanticModel) =>
        (
            from expression in _attributesService.GetAttributeArgumentExpressions(AttributeKind.Type, type)
            let typeExpression = (expression as TypeOfExpressionSyntax)?.Type
            where typeExpression != null
            let typeSymbol = semanticModel.GetTypeInfo(typeExpression).Type
            where typeSymbol != null
            select typeSymbol).FirstOrDefault();

        private ExpressionSyntax CreateObject(IMethodSymbol ctor, TypeDescription typeDescription, SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            var typeSyntax = typeDescription.Type.ToTypeSyntax(typeDescription.SemanticModel);
            if (typeSyntax.IsKind(SyntaxKind.TupleType))
            {
                return SyntaxFactory.TupleExpression()
                    .WithArguments(arguments);
            }

            if (ctor.GetAttributes(typeof(ObsoleteAttribute), typeDescription.SemanticModel).Any())
            {
                _diagnostic.Warning(Diagnostics.CtorIsObsoleted, $"Constructor {ctor} is obsoleted.", typeDescription.Binding.Location);
            }

            return SyntaxFactory.ObjectCreationExpression(typeSyntax)
                .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        }
    }
}