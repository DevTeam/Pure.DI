namespace Pure.DI.Core.Code;

interface ICycleTools
{
    DependencyNode? GetCyclicNode(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode node);
}