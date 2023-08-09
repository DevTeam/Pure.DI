namespace Pure.DI.Core;

internal interface IPathfinder
{
    IEnumerable<(int pathId, Dependency dependency)> GetPaths(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode rootNode);
}