namespace Pure.DI.Core;

interface IGraphOverride
{
    IGraph<DependencyNode, Dependency> Override(
        MdSetup setup,
        IGraph<DependencyNode, Dependency> graph,
        ref int maxId);
}