namespace Pure.DI.Core;

internal interface IDependencyGraphBuilder
{
    IEnumerable<DependencyNode> TryBuild(
        MdSetup setup,
        IReadOnlyCollection<IProcessingNode> nodes,
        out DependencyGraph? dependencyGraph);
}