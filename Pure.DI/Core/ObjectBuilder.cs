namespace Pure.DI.Core
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class ObjectBuilder : IObjectBuilder
    {
        private readonly IConstructorsResolver _constructorsResolver;

        public ObjectBuilder(IConstructorsResolver constructorsResolver) =>
            _constructorsResolver = constructorsResolver ?? throw new ArgumentNullException(nameof(constructorsResolver));

        [CanBeNull]
        public ObjectCreationExpressionSyntax TryBuild(ITypeSymbol typeSymbol, SemanticModel semanticModel, ITypeResolver typeResolver)
        {
            var resolvedType = typeResolver.Resolve(typeSymbol);
            return (
                from ctor in _constructorsResolver.Resolve(resolvedType, semanticModel)
                let parameters =
                    from parameter in ctor.Parameters
                    select TryBuild(parameter.Type, semanticModel, typeResolver)
                where parameters.All(i => i != null)
                let arguments = SyntaxFactory.SeparatedList(
                    from parameter in parameters
                    select SyntaxFactory.Argument(parameter))
                let objectCreationExpression =
                    SyntaxFactory.ObjectCreationExpression(resolvedType.ToTypeSyntax(semanticModel))
                        .WithArgumentList(SyntaxFactory.ArgumentList(arguments))
                select objectCreationExpression.NormalizeWhitespace()
            ).FirstOrDefault();
        }
    }
}