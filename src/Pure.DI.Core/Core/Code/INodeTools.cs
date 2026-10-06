namespace Pure.DI.Core.Code;

interface INodeTools
{
    bool IsLazy(DependencyNode node, DependencyGraph graph);

    bool IsBlock(IDependencyNode node);

    /// <summary>
    /// Determines whether the node is a shared (Singleton or Scoped) instance created in place.
    /// Such an instance outlives the resolve that happens to create it first,
    /// so its construction does not feed the per-resolve accumulators of that resolve.
    /// </summary>
    bool IsSharedInstance(DependencyNode node, DependencyGraph graph);

    bool IsDisposableAny(DependencyNode node);

    bool IsDisposable(DependencyNode node);

    bool IsAsyncDisposable(DependencyNode node);

    int EstimateBodyCost(IDependencyNode node, bool isLockRequired);
}