// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable InvertIf
namespace Pure.DI.Core;

internal sealed class Graph<TVertex, TEdge> : IGraph<TVertex, TEdge>
    where TEdge : IEdge<TVertex>
    where TVertex : notnull
{
    private readonly IEqualityComparer<TEdge> _egeEqualityComparer;
    private readonly IEqualityComparer<TVertex> _vertexEqualityComparer;
    private readonly ImmutableDictionary<TVertex, GraphEntry<TVertex, TEdge>> _vertexEdges;

    public Graph(
        ImmutableArray<GraphEntry<TVertex, TEdge>> entries,
        IEqualityComparer<TEdge>? egeEqualityComparer = default,
        IEqualityComparer<TVertex>? vertexEqualityComparer = default)
    {
        _egeEqualityComparer = egeEqualityComparer ?? EqualityComparer<TEdge>.Default;
        _vertexEqualityComparer = vertexEqualityComparer ?? EqualityComparer<TVertex>.Default;
        var vertexEdgesBuilder = ImmutableDictionary.CreateBuilder<TVertex, GraphEntry<TVertex, TEdge>>();
        foreach (var entry in entries)
        {
            vertexEdgesBuilder.Add(entry.Target, entry);
        }

        _vertexEdges = vertexEdgesBuilder.ToImmutable();
    }

    public int VertexCount => _vertexEdges.Count;

    public IEnumerable<TVertex> Vertices => _vertexEdges.Keys;

    public bool ContainsVertex(in TVertex vertex) => _vertexEdges.ContainsKey(vertex);

    public IEnumerable<TEdge> Edges
    {
        get
        {
            var edges = new List<TEdge>();
            foreach (var vertexEdge in _vertexEdges)
            {
                foreach (var edge in vertexEdge.Value.Edges)
                {
                    edges.Add(edge);   
                }
            }
            
            return edges;
        }
    }

    public bool ContainsEdge(in TEdge edge)
    {
        if (!_vertexEdges.TryGetValue(edge.Source, out var inOutEdges))
        {
            return false;
        }
        
        foreach (var outEdge in inOutEdges.Edges)
        {
            if (_egeEqualityComparer.Equals(outEdge, edge))
            {
                return true;
            }
        }

        return false;
    }

    public bool ContainsEdge(in TVertex source, in TVertex target) =>
        TryGetEdge(source, target, out _);

    public bool TryGetEdge(in TVertex source, in TVertex target, [NotNullWhen(true)] out TEdge? edge)
    {
        if (_vertexEdges.TryGetValue(source, out var inOutEdges))
        {
            foreach (var outEdge in inOutEdges.Edges)
            {
                if (_vertexEqualityComparer.Equals(outEdge.Target, target))
                {
                    edge = outEdge;
                    return true;
                }
            }
        }

        edge = default;
        return false;
    }

    public bool TryGetEdges(in TVertex source, in TVertex target, out ImmutableArray<TEdge> edges)
    {
        if (_vertexEdges.TryGetValue(target, out var inOutEdges))
        {
            var edgesBuilder = ImmutableArray.CreateBuilder<TEdge>();
            foreach (var outEdge in inOutEdges.Edges)
            {
                if (_vertexEqualityComparer.Equals(outEdge.Source, source))
                {
                    edgesBuilder.Add(outEdge);
                }
            }

            edges = edgesBuilder.ToImmutable();
            return true;
        }

        edges = ImmutableArray<TEdge>.Empty;
        return false;
    }

    public bool IsOutEdgesEmpty(in TVertex vertex) => InDegree(vertex) == 0;

    public int InDegree(in TVertex target) => 
        _vertexEdges.TryGetValue(target, out var inOutEdges) 
            ? inOutEdges.Edges.Length
            : throw new VertexNotFoundException();
    
    public bool TryGetInEdges(in TVertex target, out ImmutableArray<TEdge> edges)
    {
        if (_vertexEdges.TryGetValue(target, out var inOutEdges))
        {
            edges = inOutEdges.Edges;
            return true;
        }

        edges = ImmutableArray<TEdge>.Empty;
        return false;
    }

    public bool TryGetOutEdges(in TVertex source, out ImmutableArray<TEdge> edges)
    {
        var edgesBuilder = ImmutableArray.CreateBuilder<TEdge>();
        foreach (var vertexEdge in Edges)
        {
            if (_vertexEqualityComparer.Equals(vertexEdge.Source, source))
            {
                edgesBuilder.Add(vertexEdge);
            }
        }

        if (edgesBuilder.Count > 0)
        {
            edges = edgesBuilder.ToImmutable();
            return true;
        }
        
        edges = ImmutableArray<TEdge>.Empty;
        return false;
    }

    public TEdge OutEdge(in TVertex vertex, int index) => 
        _vertexEdges.TryGetValue(vertex, out var inOutEdges)
            ? inOutEdges.Edges[index]
            : throw new VertexNotFoundException();
}