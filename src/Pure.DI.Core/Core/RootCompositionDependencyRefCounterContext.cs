namespace Pure.DI.Core;

record RootCompositionDependencyRefCounterContext(IDependencyNode Node)
{
    public readonly Dictionary<PathKey, int> Counts = [];
}