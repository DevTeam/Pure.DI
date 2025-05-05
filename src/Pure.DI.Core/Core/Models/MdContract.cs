// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

readonly record struct MdContract(
    SemanticModel SemanticModel,
    InvocationExpressionSyntax Source,
    ITypeSymbol? ContractType,
    ContractKind Kind,
    in ImmutableArray<MdTag> Tags)
{
    public override string ToString()
    {
        var walker = new MetadataToLinesWalker(0);
        walker.VisitContract(this);
        return string.Join(Environment.NewLine, walker);
    }
}