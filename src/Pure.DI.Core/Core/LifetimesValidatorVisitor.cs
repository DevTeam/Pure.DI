namespace Pure.DI.Core;

sealed class LifetimesValidatorVisitor(
    ILogger logger,
    ILifetimeAnalyzer lifetimeAnalyzer,
    ILocationProvider locationProvider)
    : IGraphVisitor<HashSet<object>, ImmutableArray<Dependency>>
{
    public ImmutableArray<Dependency> Create(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode rootNode,
        ImmutableArray<Dependency> parent) =>
        ImmutableArray<Dependency>.Empty;

    public ImmutableArray<Dependency> Append(
        IGraph<DependencyNode, Dependency> graph,
        Dependency dependency,
        ImmutableArray<Dependency> parent = default) =>
        parent.IsDefaultOrEmpty
            ? ImmutableArray.Create(dependency)
            : parent.Add(dependency);

    public bool Visit(
        HashSet<object> errors,
        IGraph<DependencyNode, Dependency> graph,
        in ImmutableArray<Dependency> path)
    {
        var actualTargetLifetimeNode = path[0].Target;
        for (var i = 1; i < path.Length; i++)
        {
            var dependency = path[i];
            var targetNode = dependency.Target;
            if (!lifetimeAnalyzer.ValidateLifetimes(actualTargetLifetimeNode.Lifetime, targetNode.Lifetime))
            {
                if (errors.Add(new ErrorKey(actualTargetLifetimeNode, targetNode)))
                {
                    logger.CompileError(
                        string.Format(Strings.Error_Template_TypeWithLifetimeRequiresDirectOrTransitiveInjection, actualTargetLifetimeNode.Type, actualTargetLifetimeNode.Lifetime, targetNode.Type, targetNode.Lifetime),
                        ImmutableArray.Create(locationProvider.GetLocation(targetNode.Binding.Source)),
                        LogId.ErrorLifetimeDefect);
                }
            }

            if (lifetimeAnalyzer.GetActualDependencyLifetime(actualTargetLifetimeNode.Lifetime, targetNode.Lifetime) == targetNode.Lifetime)
            {
                actualTargetLifetimeNode = targetNode;
            }
        }

        return true;
    }
}