namespace Pure.DI.Core.Code;

interface ICycleTools
{
    bool IsCyclic(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode node);
}
