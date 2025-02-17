namespace Pure.DI.Core;

sealed class LifetimesValidatorVisitor(
    ILogger logger,
    ILifetimeAnalyzer lifetimeAnalyzer)
    : IGraphVisitor<HashSet<object>, ImmutableArray<DependencyNode>>
{
    public ImmutableArray<DependencyNode> Create(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode currentNode,
        ImmutableArray<DependencyNode> parent) =>
        parent.IsDefaultOrEmpty
            ? ImmutableArray.Create(currentNode)
            : parent.Add(currentNode);

    public bool Visit(
        HashSet<object> errors,
        IGraph<DependencyNode, Dependency> graph,
        in ImmutableArray<DependencyNode> path)
    {
        var actualTargetLifetimeNode = path[0];
        for (var i = 1; i < path.Length; i++)
        {
            var dependencyNode = path[i];
            if (!lifetimeAnalyzer.ValidateLifetimes(actualTargetLifetimeNode.Lifetime, dependencyNode.Lifetime))
            {
                if (errors.Add(new ErrorKey(actualTargetLifetimeNode, dependencyNode)))
                {
                    logger.CompileError($"Type {actualTargetLifetimeNode.Type} with lifetime {actualTargetLifetimeNode.Lifetime} requires direct or transitive dependency injectionion of type {dependencyNode.Type} with lifetime {dependencyNode.Lifetime}, which can lead to data leakage and inconsistent behavior.", dependencyNode.Binding.Source.GetLocation(), LogId.ErrorLifetimeDefect);
                }
            }

            if (lifetimeAnalyzer.GetActualDependencyLifetime(actualTargetLifetimeNode.Lifetime, dependencyNode.Lifetime) == dependencyNode.Lifetime)
            {
                actualTargetLifetimeNode = dependencyNode;
            }
        }

        return true;
    }
}