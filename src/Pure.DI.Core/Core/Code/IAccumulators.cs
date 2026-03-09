namespace Pure.DI.Core.Code;

interface IAccumulators
{
    IEnumerable<(MdAccumulator, Dependency)> GetAccumulators(
        DependencyGraph graph,
        IDependencyNode targetNode);

    IEnumerable<Accumulator> CreateAccumulators(
        DependencyGraph graph,
        IEnumerable<(MdAccumulator accumulator, Dependency dependency)> accumulators,
        IVarsMap varsMap);

    void BuildAccumulators(CodeContext ctx);
}