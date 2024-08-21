namespace Pure.DI.Core;

internal interface IGraphWalker<TContext, T>
{
    void Walk(
        TContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode root,
        IGraphVisitor<TContext, T> visitor,
        CancellationToken cancellationToken);
}