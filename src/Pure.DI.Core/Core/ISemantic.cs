namespace Pure.DI.Core;

interface ISemantic
{
    bool IsAccessible(ISymbol symbol);

    T? TryGetTypeSymbol<T>(SemanticModel semanticModel, SyntaxNode node)
        where T : ITypeSymbol;

    T GetTypeSymbol<T>(SemanticModel semanticModel, SyntaxNode node)
        where T : ITypeSymbol;

    T GetRequiredConstantValue<T>(SemanticModel semanticModel, SyntaxNode node, SmartTagKind smartTagKind = SmartTagKind.Unknown);

    T?[] GetConstantValues<T>(SemanticModel semanticModel, SyntaxNode node, SmartTagKind smartTagKind = SmartTagKind.Unknown);

    T? GetConstantValue<T>(SemanticModel semanticModel, SyntaxNode node, SmartTagKind smartTagKind = SmartTagKind.Unknown);

    bool IsValidNamespace(INamespaceSymbol? namespaceSymbol);
}