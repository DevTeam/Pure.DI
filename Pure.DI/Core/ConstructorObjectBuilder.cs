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
            TypeResolveDescription typeDescription,
            ICollection<BindingMetadata> additionalBindings,
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
                    let type = parameter.Type as INamedTypeSymbol
                    let paramResolveDescription = type != null ? typeResolver.Resolve(type, null): null
                    select TryBuildInternal(typeResolver, paramResolveDescription?.ObjectBuilder, paramResolveDescription, additionalBindings, level)
                where parameters.All(i => i != null)
                let arguments = SyntaxFactory.SeparatedList(
                    from parameter in parameters
                    select SyntaxFactory.Argument(parameter))
                let objectCreationExpression =
                    SyntaxFactory.ObjectCreationExpression(typeDescription.Type.ToTypeSyntax(typeDescription.SemanticModel))
                        .WithArgumentList(SyntaxFactory.ArgumentList(arguments))
                select objectCreationExpression
            ).FirstOrDefault();

            if (ctorExpression != null)
            {
                return ctorExpression;
            }

            return SyntaxFactory.ParseName($"Cannot find constructor for \"{typeDescription.Type}\".");
        }

        private ExpressionSyntax? TryBuildInternal(
            ITypeResolver typeResolver,
            IObjectBuilder? objectBuilder,
            TypeResolveDescription typeDescription,
            ICollection<BindingMetadata> additionalBindings,
            int level)
        {
            if (objectBuilder == null)
            {
                return null;
            }

            var constructedType = typeDescription.TypesMap.ConstructType(typeDescription.Type);
            if (!typeDescription.Type.Equals(constructedType, SymbolEqualityComparer.Default))
            {
                additionalBindings.Add(new BindingMetadata(typeDescription.Binding, constructedType));
            }

            if (typeDescription.IsResolved)
            {
                return objectBuilder.TryBuild(typeResolver, typeDescription, additionalBindings, level + 1);
            }

            var contractType = typeDescription.Type.ToTypeSyntax(typeDescription.SemanticModel);
            return SyntaxFactory.CastExpression(contractType,
                SyntaxFactory.InvocationExpression(SyntaxFactory.ParseName(nameof(IContext.Resolve)))
                    .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(contractType))));
        }
    }
}