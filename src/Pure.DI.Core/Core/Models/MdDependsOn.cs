// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core.Models;

internal readonly record struct MdDependsOn(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    in ImmutableArray<CompositionName> CompositionTypeNames)
{
    public override string ToString() => $"DependsOn(\"{CompositionTypeNames}\")";
}