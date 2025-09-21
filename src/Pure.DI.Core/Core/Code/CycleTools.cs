namespace Pure.DI.Core.Code;

class CycleTools : ICycleTools
{
    public DependencyNode? GetCyclicNode(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode node) =>
        GetCyclicNode(graph, node, ImmutableHashSet<DependencyNode>.Empty);

    private static DependencyNode? GetCyclicNode(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode node,
        in ImmutableHashSet<DependencyNode> processed)
    {
        if (processed.Contains(node))
        {
            return node;
        }

        var newProcessed = processed.Add(node);
        if (!graph.TryGetInEdges(node, out var dependencies))
        {
            return null;
        }

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var dependency in dependencies)
        {
            if (GetCyclicNode(graph, dependency.Source, newProcessed) is {} cyclicNode)
            {
                return cyclicNode;
            }
        }

        return null;
    }
}