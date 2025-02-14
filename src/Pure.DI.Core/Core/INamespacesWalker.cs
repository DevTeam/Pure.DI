namespace Pure.DI.Core;

internal interface INamespacesWalker
{
    NamespacesWalker Initialize(SemanticModel semanticModel);
    IReadOnlyCollection<string> GetResult();
    void Visit(SyntaxNode? node);
}