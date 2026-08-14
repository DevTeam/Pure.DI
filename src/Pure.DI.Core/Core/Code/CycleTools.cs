namespace Pure.DI.Core.Code;

class CycleTools : ICycleTools
{
    public bool IsCyclic(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode node)
    {
        var visited = new HashSet<DependencyNode> { node };
        var stack = new Stack<DependencyNode>();
        stack.Push(node);
        while (stack.Count > 0)
        {
            var currentNode = stack.Pop();
            if (!graph.TryGetInEdges(currentNode, out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies)
            {
                var source = dependency.Source;
                if (source == node)
                {
                    return true;
                }

                if (visited.Add(source))
                {
                    stack.Push(source);
                }
            }
        }

        return false;
    }
}
