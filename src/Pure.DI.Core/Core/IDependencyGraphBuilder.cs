namespace Pure.DI.Core;

interface IDependencyGraphBuilder
{
    IEnumerable<DependencyNode> TryBuild(
        MdSetup setup,
        IReadOnlyCollection<IProcessingNode> nodes,
        ICache<ProcessingNodeKey, IProcessingNode> nodesCache,
        out IGraph<DependencyNode, Dependency>? dependencyGraph);
}