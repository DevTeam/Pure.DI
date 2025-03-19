// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

record DependencyGraph(
    in MdSetup Source,
    IGraph<DependencyNode, Dependency> Graph,
    ImmutableArray<Root> Roots = default)
{
    private readonly Lazy<bool> _isResolved = new(() => Graph.Edges.All(i => i.IsResolved));

    public bool IsResolved => _isResolved.Value;
}