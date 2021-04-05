namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class ConstructorObjectBuilder : IObjectBuilder
    {
        private readonly IConstructorsResolver _constructorsResolver;

        public ConstructorObjectBuilder(IConstructorsResolver constructorsResolver) =>
            _constructorsResolver = constructorsResolver ?? throw new ArgumentNullException(nameof(constructorsResolver));

        public ExpressionSyntax TryBuild(
            ITypeResolver typeResolver,
            IBindingExpressionStrategy bindingExpressionStrategy,
            TypeResolveDescription typeDescription,
            ISet<BindingMetadata> additionalBindings,
            int level = 0)
        {
            if (level > 256)
            {
                return SyntaxFactory.ParseName("Cyclic dependency in \"{typeDescription.Type}\".");
            }

            var ctorExpression = (
                from ctor in _constructorsResolver.Resolve(typeResolver, typeDescription)
                let parameters =
                    from parameter in ctor.Parameters
                    let paramResolveDescription = typeResolver.Resolve(parameter.Type, null)
                    select TryBuildInternal(typeResolver, bindingExpressionStrategy, paramResolveDescription?.ObjectBuilder, paramResolveDescription, additionalBindings, level)
                where parameters.All(i => i != null)
                let arguments = SyntaxFactory.SeparatedList(
                    from parameter in parameters
                    select SyntaxFactory.Argument(parameter))
                let objectCreationExpression = CreateObject(typeDescription, arguments)
                select objectCreationExpression
            ).FirstOrDefault();

            if (ctorExpression != null)
            {
                return ctorExpression;
            }

            return SyntaxFactory.ParseName($"Cannot find constructor for \"{typeDescription.Type}\".");
        }

        private static ExpressionSyntax CreateObject(TypeResolveDescription typeDescription, SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            var typeSyntax = typeDescription.Type.ToTypeSyntax(typeDescription.SemanticModel);
            if (typeSyntax.IsKind(SyntaxKind.TupleType))
            {
                return SyntaxFactory.TupleExpression()
                    .WithArguments(arguments);
            }

            return SyntaxFactory.ObjectCreationExpression(typeSyntax)
                .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        }

        private static ExpressionSyntax? TryBuildInternal(
            ITypeResolver typeResolver,
            IBindingExpressionStrategy bindingExpressionStrategy,
            IObjectBuilder? objectBuilder,
            TypeResolveDescription typeDescription,
            ISet<BindingMetadata> additionalBindings,
            int level)
        {
            if (objectBuilder == null)
            {
                return null;
            }

            if (typeDescription.Type is INamedTypeSymbol type)
            {
                var constructedType = typeDescription.TypesMap.ConstructType(type);
                if (!typeDescription.Type.Equals(constructedType, SymbolEqualityComparer.Default))
                {
                    additionalBindings.Add(new BindingMetadata(typeDescription.Binding, constructedType));
                }

                if (typeDescription.IsResolved)
                {
                    return bindingExpressionStrategy.TryBuild(constructedType, null, additionalBindings);
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
                    return bindingExpressionStrategy.TryBuild(arrayTypeDescription, additionalBindings);
                }
            }

            return null;
        }
    }
}