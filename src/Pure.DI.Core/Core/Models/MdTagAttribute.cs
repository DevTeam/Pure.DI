// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable UnusedMember.Global
namespace Pure.DI.Core.Models;

internal readonly record struct MdTagAttribute(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    ITypeSymbol AttributeType,
    int ArgumentPosition) : IMdAttribute
{
    public override string ToString() => $".TagAttribute<{AttributeType}>({(ArgumentPosition != 0 ? ArgumentPosition.ToString() : string.Empty)})";
}