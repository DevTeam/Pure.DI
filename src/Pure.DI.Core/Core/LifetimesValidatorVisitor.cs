namespace Pure.DI.Core;

sealed class LifetimesValidatorVisitor(
    ILogger logger,
    ILifetimeAnalyzer lifetimeAnalyzer,
    ILocationProvider locationProvider)
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
                    logger.CompileError(string.Format(Strings.Error_Template_TypeWithLifetimeRequiresDirectOrTransitiveInjection, actualTargetLifetimeNode.Type, actualTargetLifetimeNode.Lifetime, dependencyNode.Type, dependencyNode.Lifetime), locationProvider.GetLocation(dependencyNode.Binding.Source), LogId.ErrorLifetimeDefect);
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