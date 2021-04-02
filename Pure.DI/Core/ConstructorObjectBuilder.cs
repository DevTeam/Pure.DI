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

        [CanBeNull]
        public ExpressionSyntax TryBuild(
            TypeResolveDescription typeDescription,
            ICollection<BindingMetadata> additionalBindings,
            int level = 0)
        {
            if (level > 256)
            {
                return null;
            }

            return (
                from ctor in _constructorsResolver.Resolve(typeDescription)
                let parameters =
                    from parameter in ctor.Parameters
                    let type = parameter.Type as INamedTypeSymbol
                    let paramResolveDescription = type != null ? typeDescription.TypeResolver.Resolve(type, null): null
                    select TryBuildInternal(paramResolveDescription?.ObjectBuilder, paramResolveDescription, additionalBindings, level)
                where parameters.All(i => i != null)
                let arguments = SyntaxFactory.SeparatedList(
                    from parameter in parameters
                    select SyntaxFactory.Argument(parameter))
                let objectCreationExpression =
                    SyntaxFactory.ObjectCreationExpression(typeDescription.Type.ToTypeSyntax(typeDescription.SemanticModel))
                        .WithArgumentList(SyntaxFactory.ArgumentList(arguments))
                select objectCreationExpression
            ).FirstOrDefault();
        }

        [CanBeNull]
        private ExpressionSyntax TryBuildInternal(
            [CanBeNull] IObjectBuilder objectBuilder,
            TypeResolveDescription typeDescription,
            ICollection<BindingMetadata> additionalBindings,
            int level)
        {
            if (objectBuilder == null)
            {
                return null;
            }

            var constructedType = typeDescription.TypesMap.ConstructType(typeDescription.Type);
            if (!typeDescription.Type.Equals(constructedType, SymbolEqualityComparer.IncludeNullability))
            {
                additionalBindings.Add(new BindingMetadata(typeDescription.Binding, constructedType));
            }

            return objectBuilder.TryBuild(typeDescription, additionalBindings, level + 1);
        }
    }
}