// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class GraphWalker<TContext, T> : IGraphWalker<TContext, T>
{
    public void Walk(
        TContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode root,
        IGraphVisitor<TContext, T> visitor,
        CancellationToken cancellationToken)
    {
        HashSet<DependencyNode> processedNodes = [];
        var nodeInfos = new Stack<NodeInfo>();
        nodeInfos.Push(new NodeInfo(root, visitor.Create(graph, root)));
        while (nodeInfos.TryPop(out var nodeInfo))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (!graph.TryGetInEdges(nodeInfo.Node, out var dependencies))
            {
                continue;
            }

            foreach (var (isResolved, dependencyNode, _, _) in dependencies)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (!isResolved)
                {
                    continue;
                }

                var visitingInfo = visitor.Create(graph, dependencyNode, nodeInfo.Info);
                if (!visitor.Visit(ctx, graph, visitingInfo))
                {
                    continue;
                }

                if (processedNodes.Add(dependencyNode))
                {
                    nodeInfos.Push(new NodeInfo(dependencyNode, visitingInfo));
                }
            }
        }
    }

    private readonly record struct NodeInfo(DependencyNode Node, T Info);
}