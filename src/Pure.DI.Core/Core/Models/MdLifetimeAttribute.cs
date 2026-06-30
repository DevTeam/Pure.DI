// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable UnusedMember.Global

namespace Pure.DI.Core.Models;

readonly record struct MdLifetimeAttribute(
    SemanticModel SemanticModel,
    ExpressionSyntax Source,
    INamedTypeSymbol AttributeType,
    int ArgumentPosition) : IMdAttribute
{
    public override string ToString() => $".LifetimeAttribute<{AttributeType}>({(ArgumentPosition != 0 ? ArgumentPosition.ToString() : string.Empty)})";
}
