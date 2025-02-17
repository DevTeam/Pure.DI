// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

readonly record struct MdDependsOn(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    in ImmutableArray<CompositionName> CompositionTypeNames)
{
    public override string ToString() => $"DependsOn(\"{string.Join(", ", CompositionTypeNames.Select(i => $"\"{i.FullName}\""))}\")";
}