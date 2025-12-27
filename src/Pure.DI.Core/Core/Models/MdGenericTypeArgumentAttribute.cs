// ReSharper disable HeapView.ObjectAllocation

namespace Pure.DI.Core.Models;

readonly record struct MdGenericTypeArgumentAttribute(
    SemanticModel SemanticModel,
    ExpressionSyntax Source,
    INamedTypeSymbol AttributeType,
    int ArgumentPosition = 0) : IMdAttribute
{
    public override string ToString() => $".GenericTypeArgumentAttribute<{AttributeType}>()";
}