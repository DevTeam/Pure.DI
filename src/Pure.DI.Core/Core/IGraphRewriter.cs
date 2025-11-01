namespace Pure.DI.Core;

interface IGraphRewriter
{
    IGraph<DependencyNode, Dependency> Rewrite(
        MdSetup setup,
        IGraph<DependencyNode, Dependency> graph,
        ref int bindingId);
}