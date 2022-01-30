namespace Pure.DI.Core;

internal interface IMetadataWalker
{
    IEnumerable<ResolverMetadata> Metadata { get; }

    void Visit(SyntaxNode? node);
}