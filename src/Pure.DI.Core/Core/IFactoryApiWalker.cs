namespace Pure.DI.Core;

interface IFactoryApiWalker
{
    IReadOnlyCollection<FactoryMeta> Meta{ get; }

    void Visit(SyntaxNode? node);
}