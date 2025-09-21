// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class LifetimeOptimizer(
    IGraphWalker<RootCompositionDependencyRefCounterContext, ImmutableArray<DependencyNode>> graphWalker,
    IGraphVisitor<RootCompositionDependencyRefCounterContext, ImmutableArray<DependencyNode>> visitor,
    CancellationToken cancellationToken)
    : ILifetimeOptimizer
{
    public Lifetime Optimize(Root root, DependencyGraph graph, IDependencyNode node, StringBuilder trace)
    {
        if (!IsOptimizable(node))
        {
            return node.Lifetime;
        }

        var refCount = GetRefCount(root, graph, node);
#if DEBUG
        trace.Append($"Ref count: {(refCount.HasValue ? refCount > 1 ? ">1" : refCount.ToString() : "empty")}");
#endif
        if (refCount is not <= 1)
        {
            return node.Lifetime;
        }

#if DEBUG
        trace.Append($", Lifetime optimization: {node.Lifetime} -> Transient");
#endif
        return Lifetime.Transient;
    }

    private static bool IsOptimizable(IDependencyNode node) =>
        node.Lifetime is Lifetime.PerResolve or Lifetime.PerBlock
        && node.Construct is not { Source.Kind: MdConstructKind.Accumulator };

    private int? GetRefCount(Root root, DependencyGraph dependencyGraph, IDependencyNode node)
    {
        if (!dependencyGraph.IsResolved)
        {
            return null;
        }

        var graph = dependencyGraph.Graph;
        var ctx = new RootCompositionDependencyRefCounterContext(node);
        graphWalker.Walk(
            ctx,
            graph,
            root.Node,
            visitor,
            cancellationToken);

        return ctx.Counts.Count == 0 ? null : ctx.Counts.Values.Max();
    }
}