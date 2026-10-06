namespace Pure.DI.Core.Code;

interface IAccumulators
{
    IEnumerable<(MdAccumulator, Dependency)> GetAccumulators(
        DependencyGraph graph,
        IDependencyNode targetNode);

    IEnumerable<(MdAccumulator, Dependency)> GetBoundaryAccumulators(
        DependencyGraph graph,
        IDependencyNode targetNode);

    IEnumerable<ITypeSymbol> GetNestedBoundaryAccumulatorTypes(
        DependencyGraph graph,
        IDependencyNode targetNode);

    IEnumerable<Accumulator> CreateAccumulators(
        DependencyGraph graph,
        IDependencyNode targetNode,
        IEnumerable<(MdAccumulator accumulator, Dependency dependency)> accumulators,
        IVarsMap varsMap);

    void BuildAccumulators(CodeContext ctx, bool includeDeclared = false);

    /// <summary>
    /// Determines whether the node or the dependencies constructed together with it
    /// inject one of the accumulators directly.
    /// </summary>
    bool InjectsAccumulator(
        DependencyGraph graph,
        IDependencyNode targetNode,
        ImmutableHashSet<int> accumulatorBindingIds);
}
