// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core.Models;

internal readonly record struct MdLifetime(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    object Lifetime)
{
    public override string ToString() => $"As({Lifetime})";
}