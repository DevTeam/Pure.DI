// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core.Models;

internal readonly record struct MdGenericTypeArgumentAttribute(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    INamedTypeSymbol AttributeType,
    int ArgumentPosition = 0) : IMdAttribute
{
    public override string ToString() => $".MdGenericTypeArgumentAttribute<{AttributeType}>()";
}