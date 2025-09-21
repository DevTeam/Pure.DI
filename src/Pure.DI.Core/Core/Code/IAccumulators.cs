namespace Pure.DI.Core.Code;

interface IAccumulators
{
    IEnumerable<(MdAccumulator, Dependency)> GetAccumulators(
        IGraph<DependencyNode, Dependency> graph,
        IDependencyNode targetNode);

    IEnumerable<Accumulator> CreateAccumulators(
        DependencyGraph graph,
        Root root,
        IEnumerable<(MdAccumulator accumulator, Dependency dependency)> accumulators,
        IVarsMap varsMap);

    void BuildAccumulators(CodeContext ctx);
}