// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core.Models;

internal readonly record struct MdGenericTypeArgument(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    INamedTypeSymbol Type)
{
    public override string ToString() => $".GenericTypeArgument<{Type}>()";
}