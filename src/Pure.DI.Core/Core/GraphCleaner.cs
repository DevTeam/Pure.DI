namespace Pure.DI.Core;

class GraphCleaner(
    CancellationToken cancellationToken)
    : IGraphRewriter
{
    public IGraph<DependencyNode, Dependency> Rewrite(
        MdSetup setup,
        IGraph<DependencyNode, Dependency> graph,
        ref int maxId)
    {
        var existingEntries = new List<GraphEntry<DependencyNode, Dependency>>();
        var processedNodes = new HashSet<DependencyNode>();
        foreach (var rootNode in from node in graph.Vertices where node.Root is not null select node)
        {
            Clean(processedNodes, graph, rootNode, existingEntries);
            if (cancellationToken.IsCancellationRequested)
            {
                return graph;
            }
        }

        return new Graph<DependencyNode, Dependency>(existingEntries);
    }

    private void Clean(
        HashSet<DependencyNode> processedNodes,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode targetNode,
        List<GraphEntry<DependencyNode, Dependency>> existingEntries)
    {
        if (!processedNodes.Add(targetNode))
        {
            return;
        }

        if (!graph.TryGetInEdges(targetNode, out var dependencies))
        {
            return;
        }

        existingEntries.Add(new GraphEntry<DependencyNode, Dependency>(targetNode, dependencies));
        foreach (var dependency in dependencies)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Clean(processedNodes, graph, dependency.Source, existingEntries);
        }
    }
}