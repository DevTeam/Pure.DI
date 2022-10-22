namespace Pure.DI.Core;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class ArrayObjectBuilder : IObjectBuilder
{
    private readonly ITypeResolver _typeResolver;

    public ArrayObjectBuilder(ITypeResolver typeResolver) => _typeResolver = typeResolver;

    public Optional<ExpressionSyntax> TryBuild(IBuildStrategy buildStrategy, Dependency dependency)
    {
        var objectCreationExpressions = new List<ExpressionSyntax>();
        if (dependency.Implementation.Type is not IArrayTypeSymbol arrayTypeSymbol)
        {
            return SyntaxFactory.ImplicitArrayCreationExpression(SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression)
                .AddExpressions(objectCreationExpressions.ToArray()));
        }

        var elementType = new SemanticType(arrayTypeSymbol.ElementType, dependency.Implementation);
        var elements =
            from element in _typeResolver.Resolve(elementType)
            let objectCreationExpression = buildStrategy.TryBuild(element, elementType)
            where objectCreationExpression.HasValue
            select objectCreationExpression.Value;

        objectCreationExpressions.AddRange(elements);

        return SyntaxRepo.ArrayCreationExpression(
                SyntaxFactory.ArrayType(new SemanticType(arrayTypeSymbol.ElementType, dependency.Implementation).TypeSyntax))
            .AddTypeRankSpecifiers(SyntaxFactory.ArrayRankSpecifier().AddSizes(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(objectCreationExpressions.Count))))
            .WithInitializer(
                SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression)
                    .AddExpressions(objectCreationExpressions.ToArray()));
    }
}