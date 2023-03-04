namespace Pure.DI.Core;

internal readonly record struct GraphEntry<TVertex, TEdge>(
    in TVertex Vertex,
    in ImmutableArray<TEdge> Edges);