namespace Pure.DI.Core;

interface IGraph<TVertex, TEdge> where TEdge : IEdge<TVertex>
{
    IReadOnlyCollection<GraphEntry<TVertex, TEdge>> Entries { get; }

    IReadOnlyCollection<TVertex> Vertices { get; }

    IReadOnlyCollection<TEdge> Edges { get; }

    bool TryGetInEdges(in TVertex target, out IReadOnlyCollection<TEdge> edges);

    // ReSharper disable once UnusedMember.Global
    bool TryGetOutEdges(in TVertex source, out IReadOnlyCollection<TEdge> edges);
}