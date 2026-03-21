// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

sealed class GraphWalker<TContext, T>(INodeTools nodeTools)
    : IGraphWalker<TContext, T>
{
    public T Walk(
        TContext ctx,
        DependencyGraph dependencyGraph,
        DependencyNode root,
        IGraphVisitor<TContext, T> visitor,
        CancellationToken cancellationToken)
    {
        HashSet<ProcessedKey> processed = [];
        var nodeInfos = new Stack<NodeInfo>();
        var graph = dependencyGraph.Graph;
        var visitingInfo = visitor.Create(ctx, dependencyGraph, root);
        if (!visitor.Visit(ctx, dependencyGraph, visitingInfo))
        {
            return visitingInfo;
        }

        nodeInfos.Push(new NodeInfo(root, visitingInfo, ImmutableArray<int>.Empty));
        while (nodeInfos.TryPop(out var nodeInfo) && !cancellationToken.IsCancellationRequested)
        {
            if (!graph.TryGetInEdges(nodeInfo.Node, out var dependencies))
            {
                continue;
            }

            var depIndex = 0;
            foreach (var dependency in dependencies)
            {
                if (!dependency.IsResolved)
                {
                    continue;
                }

                visitingInfo = visitor.AppendDependency(ctx, dependencyGraph, dependency, nodeInfo.Info);
                if (!visitor.Visit(ctx, dependencyGraph, visitingInfo))
                {
                    return visitingInfo;
                }

                var isLazy = nodeTools.IsLazy(dependency.Source, dependencyGraph);
                var depIndices = isLazy ? nodeInfo.DepIndices.Add(depIndex++) : ImmutableArray.Create(depIndex++);
                var processedKey = new ProcessedKey(dependency.Target, dependency.Source, depIndices);
                if (processed.Add(processedKey))
                {
                     nodeInfos.Push(new NodeInfo(dependency.Source, visitingInfo, depIndices));
                }
            }
        }

        return visitingInfo;
    }

    private readonly record struct NodeInfo(DependencyNode Node, T Info, in ImmutableArray<int> DepIndices);

    private readonly record struct ProcessedKey(DependencyNode Target, DependencyNode Source, in ImmutableArray<int> DepIndices)
    {
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Target.GetHashCode();
                hashCode = hashCode * 397 ^ Source.GetHashCode();
                foreach (var depIndex in DepIndices)
                {
                    hashCode = hashCode * 397 ^ depIndex;
                }

                return hashCode;
            }
        }

        public bool Equals(ProcessedKey other) =>
            Target.Equals(other.Target) &&
            Source.Equals(other.Source) &&
            DepIndices.SequenceEqual(other.DepIndices);
    }
}