namespace Pure.DI.Core;

interface IMetadataWalker
{
    void Visit(IMetadataVisitor metadataVisitor, in SyntaxUpdate update);
}