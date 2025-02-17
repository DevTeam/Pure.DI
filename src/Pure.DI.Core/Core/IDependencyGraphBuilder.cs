namespace Pure.DI.Core;

interface IDependencyGraphBuilder
{
    IEnumerable<DependencyNode> TryBuild(
        MdSetup setup,
        IReadOnlyCollection<IProcessingNode> nodes,
        out DependencyGraph? dependencyGraph);
}