namespace Pure.DI.Core;

internal interface ISemantic
{
    bool IsAccessible(ISymbol symbol);

    T? TryGetTypeSymbol<T>(SemanticModel semanticModel, SyntaxNode node)
        where T : ITypeSymbol;

    T GetTypeSymbol<T>(SemanticModel semanticModel, SyntaxNode node)
        where T : ITypeSymbol;

    T GetRequiredConstantValue<T>(SemanticModel semanticModel, SyntaxNode node);

    T?[] GetConstantValues<T>(SemanticModel semanticModel, SyntaxNode node);

    T? GetConstantValue<T>(SemanticModel semanticModel, SyntaxNode node);
}