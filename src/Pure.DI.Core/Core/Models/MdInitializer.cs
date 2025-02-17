// ReSharper disable HeapView.ObjectAllocation

namespace Pure.DI.Core.Models;

readonly record struct MdInitializer(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    ITypeSymbol Type,
    ExpressionSyntax Target)
{
    public override string ToString() => $"Initialize<{Type}>({Target})";
}