// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

readonly record struct MdGenericTypeArgument(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    INamedTypeSymbol Type)
{
    public override string ToString() => $".GenericTypeArgument<{Type}>()";
}