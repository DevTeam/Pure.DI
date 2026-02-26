namespace Pure.DI.Core;

interface IDependencyGraphBuilder
{
    IEnumerable<DependencyNode> TryBuild(GraphBuildContext ctx);
}