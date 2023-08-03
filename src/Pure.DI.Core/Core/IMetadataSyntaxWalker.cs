namespace Pure.DI.Core;

internal interface IMetadataSyntaxWalker
{
    void Visit(IMetadataVisitor metadataVisitor, in SyntaxUpdate update);
}