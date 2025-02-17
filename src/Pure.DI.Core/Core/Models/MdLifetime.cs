// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

readonly record struct MdLifetime(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    Lifetime Value)
{
    public override string ToString() => $"As({Value})";
}