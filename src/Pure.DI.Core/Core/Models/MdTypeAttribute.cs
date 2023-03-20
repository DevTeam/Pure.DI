// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core.Models;

internal readonly record struct MdTypeAttribute(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    ITypeSymbol AttributeType,
    int ArgumentPosition) : IMdAttribute
{
    public override string ToString() => $".TypeAttribute<{AttributeType}>({(ArgumentPosition != 0 ? ArgumentPosition.ToString() : string.Empty)})";
}