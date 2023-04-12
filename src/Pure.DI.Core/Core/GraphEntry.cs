namespace Pure.DI.Core;

internal readonly record struct GraphEntry<TVertex, TEdge>(
    in TVertex Target,
    in IReadOnlyCollection<TEdge> Edges);