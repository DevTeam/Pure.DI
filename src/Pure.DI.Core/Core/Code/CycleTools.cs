namespace Pure.DI.Core.Code;

class CycleTools : ICycleTools
{
    public bool IsCyclic(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode node) =>
        IsCyclic(graph, node, _ => true);

    public bool IsCyclic(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode node,
        Func<DependencyNode, bool> canPass) =>
        Reaches(graph, node, node, canPass);

    public bool DependsOn(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode from,
        DependencyNode to) =>
        from == to || Reaches(graph, from, to, _ => true);

    private static bool Reaches(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode from,
        DependencyNode to,
        Func<DependencyNode, bool> canPass)
    {
        var visited = new HashSet<DependencyNode> { from };
        var stack = new Stack<DependencyNode>();
        stack.Push(from);
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
                if (source == to)
                {
                    return true;
                }

                if (canPass(source) && visited.Add(source))
                {
                    stack.Push(source);
                }
            }
        }

        return false;
    }
}
