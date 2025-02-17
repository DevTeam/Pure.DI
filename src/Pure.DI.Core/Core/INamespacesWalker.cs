namespace Pure.DI.Core;

interface INamespacesWalker
{
    INamespacesWalker Initialize(SemanticModel semanticModel);

    IReadOnlyCollection<string> GetResult();

    void Visit(SyntaxNode? node);
}