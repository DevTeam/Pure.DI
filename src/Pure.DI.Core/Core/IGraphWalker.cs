namespace Pure.DI.Core;

interface IGraphWalker<TContext, T>
{
    T Walk(
        TContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode root,
        IGraphVisitor<TContext, T> visitor,
        CancellationToken cancellationToken);
}