namespace Pure.DI.Core;

readonly record struct GraphEntry<TVertex, TEdge>(
    in TVertex Target,
    in IReadOnlyCollection<TEdge> Edges);