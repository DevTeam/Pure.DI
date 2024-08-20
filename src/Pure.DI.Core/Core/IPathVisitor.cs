namespace Pure.DI.Core;

internal interface IPathVisitor<in TContext>
{
    bool Visit(TContext errors, in ImmutableArray<DependencyNode> path);
}