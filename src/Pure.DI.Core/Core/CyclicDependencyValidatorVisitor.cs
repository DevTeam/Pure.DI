namespace Pure.DI.Core;

internal class CyclicDependencyValidatorVisitor(
    ILogger<CyclicDependencyValidatorVisitor> logger,
    INodeInfo nodeInfo)
    : IPathVisitor<HashSet<object>>
{
    public bool Visit(HashSet<object> errors, in ImmutableArray<DependencyNode> path)
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
        }

        return result;
    }
}