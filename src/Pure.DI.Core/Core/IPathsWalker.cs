namespace Pure.DI.Core;

internal interface IPathsWalker<TContext>
{
    void Walk(
        TContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode root,
        IPathVisitor<TContext> visitor,
        CancellationToken cancellationToken);
}