namespace Pure.DI.Core;

internal interface IPathVisitor<in TContext>
{
    bool Visit(TContext ctx, in ImmutableArray<DependencyNode> path);
}