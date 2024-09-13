namespace Pure.DI.Core;

internal interface IGraph<TVertex, TEdge> where TEdge : IEdge<TVertex>
{
    IEnumerable<TVertex> Vertices { get; }

    IEnumerable<TEdge> Edges { get; }

    bool TryGetInEdges(in TVertex target, out IReadOnlyCollection<TEdge> edges);

    // ReSharper disable once UnusedMember.Global
    bool TryGetOutEdges(in TVertex source, out IReadOnlyCollection<TEdge> edges);
}