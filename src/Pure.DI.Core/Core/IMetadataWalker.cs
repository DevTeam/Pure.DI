namespace Pure.DI.Core;

internal interface IMetadataWalker
{
    void Visit(IMetadataVisitor metadataVisitor, in SyntaxUpdate update);
}