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
        var nodeInfos = new Stack<NodeInfo>(16);
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
                var processedKey = new ProcessedKey(dependency.Target.BindingId, dependency.Source.BindingId, depIndices);
                if (processed.Add(processedKey))
                {
                     nodeInfos.Push(new NodeInfo(dependency.Source, visitingInfo, depIndices));
                }
            }
        }

        return visitingInfo;
    }

    private readonly record struct NodeInfo(DependencyNode Node, T Info, in ImmutableArray<int> DepIndices);

    private readonly record struct ProcessedKey(int TargetBindingId, int SourceBindingId, in ImmutableArray<int> DepIndices)
    {
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = TargetBindingId;
                hashCode = hashCode * 397 ^ SourceBindingId;
                // ReSharper disable once ForCanBeConvertedToForeach
                // ReSharper disable once LoopCanBeConvertedToQuery
                for (var index = 0; index < DepIndices.Length; index++)
                {
                    hashCode = hashCode * 397 ^ DepIndices[index];
                }

                return hashCode;
            }
        }

        public bool Equals(ProcessedKey other) =>
            TargetBindingId == other.TargetBindingId &&
            SourceBindingId == other.SourceBindingId &&
            DepIndices.Length == other.DepIndices.Length &&
            DepIndices.AsSpan().SequenceEqual(other.DepIndices.AsSpan());
    }
}
