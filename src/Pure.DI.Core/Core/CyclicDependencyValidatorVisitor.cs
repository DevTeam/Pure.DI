namespace Pure.DI.Core;

internal sealed class CyclicDependencyValidatorVisitor(
    ILogger logger,
    INodeInfo nodeInfo)
    : IGraphVisitor<HashSet<object>, ImmutableArray<DependencyNode>>
{
    public ImmutableArray<DependencyNode> Create(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode currentNode,
        ImmutableArray<DependencyNode> parent) =>
        parent.IsDefaultOrEmpty
            ? ImmutableArray.Create(currentNode)
            : parent.Add(currentNode);

    public bool Visit(
        HashSet<object> errors,
        IGraph<DependencyNode, Dependency> graph,
        in ImmutableArray<DependencyNode> path)
    {
        if (path.Length < 2)
        {
            return true;
        }

        var nodes = new HashSet<DependencyNode>();
        var result = true;
        foreach (var node in path)
        {
            if (nodeInfo.IsLazy(node))
            {
                nodes.Clear();
            }

            if (nodes.Add(node))
            {
                continue;
            }

            if (!errors.Add(path))
            {
                continue;
            }

            var pathStr = string.Join(" <-- ", path.Select(i => i.Type));
            logger.CompileError($"Cyclic dependency has been found: {pathStr}.", node.Binding.Source.GetLocation(), LogId.ErrorCyclicDependency);
            result = false;
            break;
        }

        return result;
    }
}