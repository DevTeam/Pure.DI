namespace Pure.DI.Core;

internal interface IGraph<TVertex, TEdge> where TEdge : IEdge<TVertex>
{
    int VertexCount { get; }
    
    IEnumerable<TVertex> Vertices { get; }
    
    IEnumerable<TEdge> Edges { get; }
    
    bool ContainsVertex(in TVertex vertex);
    
    bool ContainsEdge(in TEdge edge);
    
    bool ContainsEdge(in TVertex source, in TVertex target);
    
    bool TryGetEdge(in TVertex source, in TVertex target, [NotNullWhen(true)] out TEdge? edge);
    
    bool TryGetEdges(in TVertex source, in TVertex target, out ImmutableArray<TEdge> edges);
    
    bool IsOutEdgesEmpty(in TVertex vertex);
    
    int InDegree(in TVertex target);
    
    bool TryGetInEdges(in TVertex target, out ImmutableArray<TEdge> edges);

    bool TryGetOutEdges(in TVertex source, out ImmutableArray<TEdge> edges);
}