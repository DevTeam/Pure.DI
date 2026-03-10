namespace Pure.DI.Core.Models;

record GraphBuildContext(
    MdSetup Setup,
    in ImmutableArray<IProcessingNode> Nodes,
    in ImmutableDictionary<ISymbol, ImmutableArray<MdAccumulator>> Accumulators)
{
    public IGraph<DependencyNode, Dependency>? Graph { get; set; }
}