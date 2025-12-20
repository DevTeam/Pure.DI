namespace Pure.DI.Core;

sealed class LifetimesValidatorVisitor(
    ILogger logger,
    ILifetimeAnalyzer lifetimeAnalyzer,
    ILocationProvider locationProvider)
    : IGraphVisitor<LifetimesValidatorContext, ImmutableArray<Dependency>>
{
    public ImmutableArray<Dependency> Create(
        LifetimesValidatorContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode rootNode,
        ImmutableArray<Dependency> parent) =>
        ImmutableArray<Dependency>.Empty;

    public ImmutableArray<Dependency> AppendDependency(
        LifetimesValidatorContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        Dependency dependency,
        ImmutableArray<Dependency> parent = default) =>
        parent.IsDefaultOrEmpty
            ? ImmutableArray.Create(dependency)
            : parent.Add(dependency);

    public bool Visit(
        LifetimesValidatorContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        in ImmutableArray<Dependency> path)
    {
        if (path.IsEmpty)
        {
            return true;
        }

        var actualTargetLifetimeNode = path[0].Target;
        // ReSharper disable once UseDeconstruction
        foreach (var dependency in path)
        {
            var targetNode = dependency.Target;
            if (!lifetimeAnalyzer.ValidateScopedToSingleton(actualTargetLifetimeNode.Lifetime, targetNode.Lifetime))
            {
                if (ctx.Errors.Add(new ScopedToSingletonErrorKey(actualTargetLifetimeNode, targetNode)))
                {
                    logger.CompileError(
                        string.Format(Strings.Error_Template_TypeWithLifetimeRequiresDirectOrTransitiveInjection, actualTargetLifetimeNode.Type, actualTargetLifetimeNode.Lifetime, targetNode.Type, targetNode.Lifetime),
                        ImmutableArray.Create(locationProvider.GetLocation(targetNode.Binding.Source)),
                        LogId.ErrorLifetimeDefect);
                }
            }

            if (!lifetimeAnalyzer.ValidateRootKindSpecificLifetime(ctx.Root, targetNode.Lifetime))
            {
                if (ctx.Errors.Add(new RootKindSpecificLifetimeErrorKey(ctx.Root, targetNode)))
                {
                    logger.CompileError(
                        string.Format(Strings.Error_Template_StaticRootCannotUseLifetime, ctx.Root.Name, ctx.Root.Source.RootType, targetNode.Lifetime),
                        dependency.Injection.Locations,
                        LogId.ErrorLifetimeDefect);
                }
            }

            var sourceNode = dependency.Source;
            if (!lifetimeAnalyzer.ValidateRootKindSpecificLifetime(ctx.Root, sourceNode.Lifetime))
            {
                if (ctx.Errors.Add(new RootKindSpecificLifetimeErrorKey(ctx.Root, sourceNode)))
                {
                    logger.CompileError(
                        string.Format(Strings.Error_Template_StaticRootCannotUseLifetime, ctx.Root.Name, ctx.Root.Source.RootType, sourceNode.Lifetime),
                        dependency.Injection.Locations,
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

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private record ScopedToSingletonErrorKey(DependencyNode TargetNode, DependencyNode SourceNode);

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private record RootKindSpecificLifetimeErrorKey(Root root, DependencyNode SourceNode);
}