// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core.Models;

internal readonly record struct MdLifetime(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    Lifetime Value)
{
    public override string ToString() => $"As({Value})";
}