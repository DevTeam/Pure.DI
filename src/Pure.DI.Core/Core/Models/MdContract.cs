// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core.Models;

internal readonly record struct MdContract(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    ITypeSymbol ContractType,
    in ImmutableArray<MdTag> Tags)
{
    public override string ToString()
    {
        var walker = new MetadataToLinesWalker(0);
        walker.VisitContract(this);
        return string.Join(Environment.NewLine, walker);
    }
}