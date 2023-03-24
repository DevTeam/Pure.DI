namespace Pure.DI.Core;

internal interface IDependencyGraphBuilder
{
    IEnumerable<ProcessingNode> TryBuild(
        MdSetup setup,
        IReadOnlyCollection<ProcessingNode> nodes,
        out DependencyGraph? dependencyGraph,
        CancellationToken cancellationToken);
}