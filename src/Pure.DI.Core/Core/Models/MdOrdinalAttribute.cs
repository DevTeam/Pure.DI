// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable UnusedMember.Global

namespace Pure.DI.Core.Models;

readonly record struct MdOrdinalAttribute(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    INamedTypeSymbol AttributeType,
    int ArgumentPosition) : IMdAttribute
{
    public override string ToString() => $".OrdinalAttribute<{AttributeType}>({(ArgumentPosition != 0 ? ArgumentPosition.ToString() : string.Empty)})";
}