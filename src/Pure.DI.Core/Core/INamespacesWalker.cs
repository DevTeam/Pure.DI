namespace Pure.DI.Core;

internal interface INamespacesWalker
{
    INamespacesWalker Initialize(SemanticModel semanticModel);

    IReadOnlyCollection<string> GetResult();

    void Visit(SyntaxNode? node);
}