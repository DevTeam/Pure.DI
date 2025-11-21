namespace Pure.DI.Core;

sealed class RootCompositionDependencyRefCounterVisitor(INodeTools nodeTools)
    : IGraphVisitor<RootCompositionDependencyRefCounterContext, ImmutableArray<DependencyNode>>
{
    public ImmutableArray<DependencyNode> Create(
        RootCompositionDependencyRefCounterContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode rootNode,
        ImmutableArray<DependencyNode> parent) =>
        ImmutableArray.Create(rootNode);

    public ImmutableArray<DependencyNode> AppendDependency(
        RootCompositionDependencyRefCounterContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        Dependency dependency,
        ImmutableArray<DependencyNode> parent = default) =>
        parent.Add(dependency.Source);

    public bool Visit(
        RootCompositionDependencyRefCounterContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        in ImmutableArray<DependencyNode> path)
    {
        var dependencyNode = path.Last();
        if (dependencyNode.BindingId != ctx.Node.BindingId)
        {
            return true;
        }

        PathKey? key;
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (dependencyNode.Lifetime)
        {
            case Lifetime.PerResolve:
                if (path.Any(nodeTools.IsLazy))
                {
                    ctx.Counts.Clear();
                    return false;
                }

                key = PathKey.Default;
                break;

            case Lifetime.PerBlock:
                var blockPath = path.Select(i => i.BindingId).ToImmutableArray();
                key = new PathKey(blockPath);
                break;

            default:
                return true;
        }

        ctx.Counts.TryGetValue(key, out var count);
        count++;
        ctx.Counts[key] = count;
        return count <= 1;
    }
}