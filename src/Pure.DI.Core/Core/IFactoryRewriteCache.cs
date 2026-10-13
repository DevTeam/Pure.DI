namespace Pure.DI.Core;

interface IFactoryRewriteCache
{
    ITypeSymbol? GetTypeSymbol(SemanticModel semanticModel, SyntaxNode node);

    TypeSyntax GetTypeSyntax(SyntaxNode node, string typeName);
}
