namespace Pure.DI.Core;

sealed class RootArgsVisitor
    : IGraphVisitor<RootArgsContext, ImmutableArray<Dependency>>
{
    public ImmutableArray<Dependency> Create(
        RootArgsContext ctx,
        DependencyGraph dependencyGraph,
        DependencyNode rootNode,
        ImmutableArray<Dependency> parent) =>
        ImmutableArray<Dependency>.Empty;

    public ImmutableArray<Dependency> AppendDependency(
        RootArgsContext ctx,
        DependencyGraph dependencyGraph,
        Dependency dependency,
        ImmutableArray<Dependency> parent = default) =>
        parent.IsDefaultOrEmpty
            ? ImmutableArray.Create(dependency)
            : parent.Add(dependency);

    public bool Visit(
        RootArgsContext ctx,
        DependencyGraph dependencyGraph,
        in ImmutableArray<Dependency> path)
    {
        if (path.IsEmpty)
        {
            return true;
        }

        var dependency = path[0];
        var source = dependency.Source;
        if (source.Arg is not null)
        {
            ctx.Args.Add(ctx.varsMap.GetInjection(dependencyGraph, dependency.Injection, source).Var.Declaration);
        }

        return true;
    }
}
