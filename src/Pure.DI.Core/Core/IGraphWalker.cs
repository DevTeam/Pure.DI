namespace Pure.DI.Core;

interface IGraphWalker<TContext, T>
{
    T Walk(
        TContext ctx,
        DependencyGraph dependencyGraph,
        DependencyNode root,
        IGraphVisitor<TContext, T> visitor,
        CancellationToken cancellationToken);
}