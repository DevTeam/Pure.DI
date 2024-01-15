// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.Core;

internal sealed class Graph<TVertex, TEdge> : IGraph<TVertex, TEdge>
    where TEdge : IEdge<TVertex>
    where TVertex : notnull
{
    private readonly List<GraphEntry<TVertex, TEdge>> _entries = new();
    private readonly Dictionary<TVertex, GraphEntry<TVertex, TEdge>> _inOutEdges;
    private readonly Dictionary<TVertex, GraphEntry<TVertex, TEdge>> _outInEdges;
    private readonly List<TEdge> _edges = [];

    public Graph(
        IEnumerable<GraphEntry<TVertex, TEdge>> entries,
        IEqualityComparer<TVertex>? vertexEqualityComparer = default)
    {
        var comparer = vertexEqualityComparer ?? EqualityComparer<TVertex>.Default;
        _inOutEdges = new Dictionary<TVertex, GraphEntry<TVertex, TEdge>>(comparer);
        var outInEdges = new Dictionary<TVertex, List<TEdge>>(comparer);
        foreach (var entry in entries)
        {
            _entries.Add(entry);
            _inOutEdges.Add(entry.Target, entry);
            foreach (var edge in entry.Edges)
            {
                if (!outInEdges.TryGetValue(edge.Source, out var vertices))
                {
                    vertices = [];
                    outInEdges.Add(edge.Source, vertices);
                }

                vertices.Add(edge);
                _edges.Add(edge);
            }
        }

        _outInEdges = outInEdges.ToDictionary(i => i.Key, i => new GraphEntry<TVertex, TEdge>(i.Key, i.Value));
    }

    public IEnumerable<TVertex> Vertices => _inOutEdges.Keys;

    public IEnumerable<TEdge> Edges => _edges;

    public bool TryGetInEdges(in TVertex target, out IReadOnlyCollection<TEdge> edges)
    {
        if (_inOutEdges.TryGetValue(target, out var inOutEdges))
        {
            edges = inOutEdges.Edges;
            return edges.Count > 0;
        }

        edges = ImmutableArray<TEdge>.Empty;
        return false;
    }

    public bool TryGetOutEdges(in TVertex source, out IReadOnlyCollection<TEdge> edges)
    {
        if (_outInEdges.TryGetValue(source, out var outInEdges))
        {
            edges = outInEdges.Edges;
            return edges.Count > 0;
        }

        edges = ImmutableArray<TEdge>.Empty;
        return false;
    }

    public IGraph<TVertex, TEdge> Consolidate(ISet<TVertex> vertices) => 
        new Graph<TVertex, TEdge>(
            _entries.Where(i => 
                vertices.Contains(i.Target)
                || vertices.Intersect(i.Edges.Select(j => j.Source)).Any()));
}