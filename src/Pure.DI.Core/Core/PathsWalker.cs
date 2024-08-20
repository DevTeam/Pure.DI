namespace Pure.DI.Core;

internal class PathsWalker<TContext> : IPathsWalker<TContext>
{
    public void Walk(
        TContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode root,
        IPathVisitor<TContext> visitor,
        CancellationToken cancellationToken)
    {
        HashSet<DependencyNode> processedNodes = [];
        var paths = new Stack<ImmutableArray<DependencyNode>>();
        paths.Push(ImmutableArray.Create(root));
        while (paths.TryPop(out var path))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            
            if (!graph.TryGetInEdges(path[^1], out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (!dependency.IsResolved)
                {
                    continue;
                }

                var visitingPath = path.Add(dependency.Source);
                if (!visitor.Visit(ctx, visitingPath))
                {
                    continue;
                }
                
                if (processedNodes.Add(dependency.Source))
                {
                    paths.Push(visitingPath);
                }
            }
        }
    }
}