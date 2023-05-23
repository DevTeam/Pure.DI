// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.Core;

internal sealed class Graph<TVertex, TEdge> : IGraph<TVertex, TEdge>
    where TEdge : IEdge<TVertex>
    where TVertex : notnull
{
    private readonly Dictionary<TVertex, GraphEntry<TVertex, TEdge>> _vertexEdges;
    private readonly IEqualityComparer<TVertex> _vertexEqualityComparer;

    public Graph(
        IEnumerable<GraphEntry<TVertex, TEdge>> entries,
        IEqualityComparer<TVertex>? vertexEqualityComparer = default)
    {
        _vertexEqualityComparer = vertexEqualityComparer ?? EqualityComparer<TVertex>.Default;
        _vertexEdges = new Dictionary<TVertex, GraphEntry<TVertex, TEdge>>(_vertexEqualityComparer);
        foreach (var entry in entries)
        {
            _vertexEdges.Add(entry.Target, entry);
        }
    }

    public IEnumerable<TVertex> Vertices => _vertexEdges.Keys;

    public IEnumerable<TEdge> Edges
    {
        get
        {
            var edges = new List<TEdge>();
            foreach (var vertexEdge in _vertexEdges)
            {
                edges.AddRange(vertexEdge.Value.Edges);
            }
            
            return edges;
        }
    }

    public bool TryGetInEdges(in TVertex target, out IReadOnlyCollection<TEdge> edges)
    {
        if (_vertexEdges.TryGetValue(target, out var inOutEdges))
        {
            edges = inOutEdges.Edges;
            return true;
        }

        edges = ImmutableArray<TEdge>.Empty;
        return false;
    }

    public bool TryGetOutEdges(in TVertex source, out IReadOnlyCollection<TEdge> edges)
    {
        var result = new List<TEdge>();
        foreach (var vertexEdge in Edges)
        {
            if (_vertexEqualityComparer.Equals(vertexEdge.Source, source))
            {
                result.Add(vertexEdge);
            }
        }

        edges = result;
        return result.Count > 0;
    }
}