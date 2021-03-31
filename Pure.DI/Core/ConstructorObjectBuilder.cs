namespace Pure.DI.Core
{
    using System;
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
            BindingMetadata binding,
            INamedTypeSymbol contractType,
            ExpressionSyntax tag,
            SemanticModel semanticModel,
            ITypeResolver typeResolver,
            int level = 0)
        {
            if (level > 256)
            {
                return null;
            }

            var typeResolveDescription = typeResolver.Resolve(contractType, tag);
            return (
                from ctor in _constructorsResolver.Resolve(typeResolveDescription.TypeSymbol, semanticModel)
                let parameters =
                    from parameter in ctor.Parameters
                    let type = parameter.Type as INamedTypeSymbol
                    let paramResolveDescription = type != null ? typeResolver.Resolve(type, null): null
                    select paramResolveDescription?.ObjectBuilder.TryBuild(paramResolveDescription.Binding, paramResolveDescription.TypeSymbol, paramResolveDescription.Binding.Tags.FirstOrDefault(), semanticModel, typeResolver, level + 1)
                where parameters.All(i => i != null)
                let arguments = SyntaxFactory.SeparatedList(
                    from parameter in parameters
                    select SyntaxFactory.Argument(parameter))
                let objectCreationExpression =
                    SyntaxFactory.ObjectCreationExpression(typeResolveDescription.TypeSymbol.ToTypeSyntax(semanticModel))
                        .WithArgumentList(SyntaxFactory.ArgumentList(arguments))
                select objectCreationExpression.NormalizeWhitespace()
            ).FirstOrDefault();
        }
    }
}