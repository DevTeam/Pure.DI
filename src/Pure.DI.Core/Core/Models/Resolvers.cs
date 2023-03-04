namespace Pure.DI.Core.Models;

internal record Resolvers(
    in ImmutableDictionary<DependencyNode, string> ClassFields,
    in ImmutableDictionary<Root, ImmutableArray<Line>> Lines,
    int DisposablesCount);