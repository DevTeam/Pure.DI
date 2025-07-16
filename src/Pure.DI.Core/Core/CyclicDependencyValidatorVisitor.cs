namespace Pure.DI.Core;

using static Lifetime;

sealed class CyclicDependencyValidatorVisitor(
    ILogger logger,
    INodeInfo nodeInfo,
    ILocationProvider locationProvider,
    ITypeResolver typeResolver)
    : IGraphVisitor<CyclicDependenciesValidatorContext, ImmutableArray<Dependency>>
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
        CyclicDependenciesValidatorContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        in ImmutableArray<Dependency> path)
    {
        if (path.Length < 2)
        {
            return true;
        }

        var nodes = new HashSet<DependencyNode>();
        var result = true;
        foreach (var dependency in path)
        {
            var source = dependency.Source;
            if (source.Lifetime is Singleton or Scoped or PerResolve or PerBlock && nodeInfo.IsLazy(source))
            {
                nodes.Clear();
            }

            if (nodes.Add(source))
            {
                continue;
            }

            if (!ctx.Errors.Add(path))
            {
                continue;
            }

            var pathStr = string.Join(" <-- ", path.Select(i => typeResolver.Resolve(ctx.DependencyGraph.Source, i.Source.Type).Name.Replace("global::", "")));
            var locations = (dependency.Injection.Locations.IsDefault ? ImmutableArray<Location>.Empty : dependency.Injection.Locations)
                .AddRange(path.SelectMany(i => i.Injection.Locations.IsDefault ? ImmutableArray<Location>.Empty : i.Injection.Locations))
                .Add(locationProvider.GetLocation(source.Binding.Source));

            logger.CompileError(string.Format(Strings.Error_Template_CyclicDependency, pathStr), locations, LogId.ErrorCyclicDependency);
            result = false;
            break;
        }

        return result;
    }
}