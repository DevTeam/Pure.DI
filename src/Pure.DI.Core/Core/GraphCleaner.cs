namespace Pure.DI.Core;

class GraphCleaner(
    CancellationToken cancellationToken)
    : IGraphRewriter
{
    public IGraph<DependencyNode, Dependency> Rewrite(
        MdSetup setup,
        IGraph<DependencyNode, Dependency> graph,
        ref int bindingId)
    {
        var existingEntries = new List<GraphEntry<DependencyNode, Dependency>>();
        var processed = new HashSet<int>();
        foreach (var rootNode in from node in graph.Vertices where node.Root is not null select node)
        {
            Clean(processed, graph, rootNode, existingEntries);
            if (cancellationToken.IsCancellationRequested)
            {
                return graph;
            }
        }

        return new Graph<DependencyNode, Dependency>(existingEntries);
    }

    private void Clean(
        HashSet<int> processed,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode targetNode,
        List<GraphEntry<DependencyNode, Dependency>> existingEntries)
    {
        var stack = new Stack<DependencyNode>(existingEntries.Count);
        stack.Push(targetNode);
        while (stack.TryPop(out var currentNode))
        {
            if (!graph.TryGetInEdges(currentNode, out var dependencies)
                || !processed.Add(currentNode.Binding.Id))
            {
                continue;
            }

            existingEntries.Add(new GraphEntry<DependencyNode, Dependency>(currentNode, dependencies));
            foreach (var dependency in dependencies.Reverse())
            {
                stack.Push(dependency.Source);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}