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

        public ExpressionSyntax Build(IBuildStrategy buildStrategy, Dependency dependency)
        {
            var objectCreationExpressions = new List<ExpressionSyntax>();
            if (dependency.Implementation.Type is not IArrayTypeSymbol arrayTypeSymbol)
            {
                return SyntaxFactory.ImplicitArrayCreationExpression(SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression)
                    .AddExpressions(objectCreationExpressions.ToArray()));
            }

            objectCreationExpressions.AddRange(
                from element in _typeResolver.Resolve(new SemanticType(arrayTypeSymbol.ElementType, dependency.Implementation))
                let objectCreationExpression = buildStrategy.Build(element)
                select objectCreationExpression);

            return SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(new SemanticType(arrayTypeSymbol.ElementType, dependency.Implementation).TypeSyntax))
                .AddTypeRankSpecifiers(SyntaxFactory.ArrayRankSpecifier().AddSizes(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(objectCreationExpressions.Count))))
                .WithInitializer(
                    SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression)
                        .AddExpressions(objectCreationExpressions.ToArray()));
        }
    }
}
