namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class ArrayObjectBuilder: IObjectBuilder
    {
        private readonly ITypeResolver _typeResolver;

        public ArrayObjectBuilder(ITypeResolver typeResolver) => _typeResolver = typeResolver;

        public ExpressionSyntax? TryBuild(IBuildStrategy buildStrategy, Dependency dependency)
        {
            var objectCreationExpressions = new List<ExpressionSyntax>();
            if (dependency.Implementation.Type is not IArrayTypeSymbol arrayTypeSymbol)
            {
                return SyntaxFactory.ImplicitArrayCreationExpression(SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression)
                    .AddExpressions(objectCreationExpressions.ToArray()));
            }

            var elementTye = new SemanticType(arrayTypeSymbol.ElementType, dependency.Implementation);
            var elements =
                from element in _typeResolver.Resolve(elementTye)
                let objectCreationExpression = buildStrategy.TryBuild(element, elementTye)
                where objectCreationExpression != null
                select (ExpressionSyntax)objectCreationExpression;

            objectCreationExpressions.AddRange(elements);

            return SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(new SemanticType(arrayTypeSymbol.ElementType, dependency.Implementation).TypeSyntax))
                .AddTypeRankSpecifiers(SyntaxFactory.ArrayRankSpecifier().AddSizes(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(objectCreationExpressions.Count))))
                .WithInitializer(
                    SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression)
                        .AddExpressions(objectCreationExpressions.ToArray()));
        }
    }
}
