// ReSharper disable HeapView.ObjectAllocation

namespace Pure.DI.Core.Models;

readonly record struct MdInitializer(
    SemanticModel SemanticModel,
    ExpressionSyntax Source,
    int Position,
    ITypeSymbol Type,
    ExpressionSyntax TargetArg,
    ImmutableArray<MdOverride> Overrides)
{
    public override string ToString() => $"Initialize<{Type}>({TargetArg})";
}