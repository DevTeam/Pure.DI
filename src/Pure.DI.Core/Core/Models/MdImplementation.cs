// ReSharper disable HeapView.ObjectAllocation

namespace Pure.DI.Core.Models;

readonly record struct MdImplementation(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    ITypeSymbol Type)
{
    public override string ToString() => $"To<{Type}>()";
}