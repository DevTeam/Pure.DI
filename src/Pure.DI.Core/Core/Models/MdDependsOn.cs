// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core.Models;

internal readonly record struct MdDependsOn(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    in ImmutableArray<string> CompositionTypeNames)
{
    public override string ToString() => $"DependsOn(\"{CompositionTypeNames}\")";
}