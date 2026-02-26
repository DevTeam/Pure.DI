namespace Pure.DI.Core.Models;

record GraphBuildContext(
    MdSetup Setup,
    IReadOnlyCollection<IProcessingNode> Nodes,
    ICache<ProcessingNodeKey, IProcessingNode> NodesCache)
{
    public IGraph<DependencyNode, Dependency>? Graph { get; set; }
}