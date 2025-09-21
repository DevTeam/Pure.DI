namespace Pure.DI.Core;

using static Lifetime;

sealed class CyclicDependencyValidatorVisitor(INodeTools nodeTools)
    : IGraphVisitor<CyclicDependenciesValidatorContext, ImmutableArray<Dependency>>
{
    public ImmutableArray<Dependency> Create(
        CyclicDependenciesValidatorContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode rootNode,
        ImmutableArray<Dependency> parent) =>
        ImmutableArray<Dependency>.Empty;

    public ImmutableArray<Dependency> AppendDependency(
        CyclicDependenciesValidatorContext ctx,
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
        foreach (var dependency in path)
        {
            var source = dependency.Source;
            if (source.Lifetime is Singleton or Scoped or PerResolve or PerBlock && nodeTools.IsLazy(source))
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

            ctx.Cyclicdependency = dependency;
            return false;
        }

        return true;
    }
}