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

    /// <summary>
    /// Determines whether the factory of the node uses the composition lock via <c>ctx.Lock</c>.
    /// </summary>
    bool UsesContextLock(DependencyNode node);

    /// <summary>
    /// Determines whether the factory of the node reads a member of its context, such as <c>ctx.ConsumerType</c>.
    /// </summary>
    bool ReadsContextMember(DependencyNode node, string memberName);

    bool IsDisposableAny(DependencyNode node);

    bool IsDisposable(DependencyNode node);

    bool IsAsyncDisposable(DependencyNode node);

    int EstimateBodyCost(IDependencyNode node, bool isLockRequired);
}