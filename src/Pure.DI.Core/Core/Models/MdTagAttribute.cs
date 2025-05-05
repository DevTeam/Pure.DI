// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable UnusedMember.Global

namespace Pure.DI.Core.Models;

readonly record struct MdTagAttribute(
    SemanticModel SemanticModel,
    InvocationExpressionSyntax Source,
    INamedTypeSymbol AttributeType,
    int ArgumentPosition) : IMdAttribute
{
    public override string ToString() => $".TagAttribute<{AttributeType}>({(ArgumentPosition != 0 ? ArgumentPosition.ToString() : string.Empty)})";
}