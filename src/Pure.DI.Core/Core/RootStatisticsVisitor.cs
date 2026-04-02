namespace Pure.DI.Core;

sealed class RootStatisticsVisitor
    : IGraphVisitor<RootStatisticsContext, DependencyNode>
{
    public DependencyNode Create(
        RootStatisticsContext ctx,
        DependencyGraph dependencyGraph,
        DependencyNode rootNode,
        DependencyNode? parent) => rootNode;

    public DependencyNode AppendDependency(
        RootStatisticsContext ctx,
        DependencyGraph dependencyGraph,
        Dependency dependency,
        DependencyNode? parent) => dependency.Source;

    public bool Visit(
        RootStatisticsContext ctx,
        DependencyGraph dependencyGraph,
        in DependencyNode node)
    {
        ctx.RegisterNode(node);
        return true;
    }
}
