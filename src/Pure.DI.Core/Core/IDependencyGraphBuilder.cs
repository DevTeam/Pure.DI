namespace Pure.DI.Core;

internal interface IDependencyGraphBuilder
{
    IEnumerable<DependencyNode> TryBuild(
        MdSetup setup,
        IReadOnlyCollection<ProcessingNode> nodes,
        out DependencyGraph? dependencyGraph,
        CancellationToken cancellationToken);
}