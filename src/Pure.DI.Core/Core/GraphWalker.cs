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

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
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

    private class ProcessedKey
    {
        private readonly DependencyNode _target;
        private readonly DependencyNode _source;
        private readonly ImmutableArray<int> _depIndices;
        private readonly int _hashCode;

        public ProcessedKey(DependencyNode target, DependencyNode source, in ImmutableArray<int> depIndices)
        {
            _target = target;
            _source = source;
            _depIndices = depIndices;
            unchecked
            {
                var hashCode = _target.GetHashCode();
                hashCode = hashCode * 397 ^ _source.GetHashCode();
                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var depIndex in _depIndices)
                {
                    hashCode = hashCode * 397 ^ depIndex;
                }

                _hashCode = hashCode;
            }
        }

        // ReSharper disable once MemberCanBePrivate.Local
        protected bool Equals(ProcessedKey other)
        {
            if(!_target.Equals(other._target) || !_source.Equals(other._source))
            {
                return false;
            }

            var length = Math.Min(_depIndices.Length, other._depIndices.Length);
            // ReSharper disable once LoopCanBeConvertedToQuery
            return _depIndices.AsSpan()[..length].SequenceEqual(other._depIndices.AsSpan()[..length]);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ProcessedKey)obj);
        }

        public override int GetHashCode() => _hashCode;
    }
}