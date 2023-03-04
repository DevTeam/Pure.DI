// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable UnusedMember.Global
namespace Pure.DI.Core.Models;

internal readonly record struct MdOrderAttribute(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    ITypeSymbol AttributeType,
    int ArgumentPosition) : IMdAttribute
{
    public override string ToString() => $".OrderAttribute<{AttributeType}>({(ArgumentPosition != 0 ? ArgumentPosition.ToString() : string.Empty)})";
}