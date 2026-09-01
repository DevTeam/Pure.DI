namespace Pure.DI.Core;

static class DependencyGraphExtensions
{
    public static bool TryGetSingleResolvedDependency(
        this DependencyGraph dependencyGraph,
        DependencyNode root,
        out DependencyNode dependencyNode)
    {
        dependencyNode = null!;
        if (!dependencyGraph.Graph.TryGetInEdges(root, out var dependencies))
        {
            return false;
        }

        var dependencyFound = false;
        foreach (var dependency in dependencies)
        {
            if (!dependency.IsResolved)
            {
                continue;
            }

            if (dependencyFound)
            {
                return false;
            }

            dependencyNode = dependency.Source;
            dependencyFound = true;
        }

        return dependencyFound;
    }
}

readonly record struct SingleDependencyValidationKey(
    IGraph<DependencyNode, Dependency> Graph,
    DependencyNode Dependency);
