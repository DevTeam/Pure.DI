namespace Pure.DI.Core.Code;

interface IRootUseSiteCounter
{
    RootUseSiteAnalysis Analyze(DependencyGraph graph, DependencyNode root);
}

record RootUseSiteAnalysis(
    IReadOnlyDictionary<int, int> UseSiteCount,
    HashSet<int> FactoryDownstream,
    // The overridden values that the code creating a node reads and does not declare itself, by binding id.
    // A node with none of them does not depend on the lambda it is reached from, see #186.
    IReadOnlyDictionary<int, ImmutableArray<MdOverride>> FreeOverrides,
    // The estimated size of the code that creates a node in place, including the dependencies created in place with it.
    IReadOnlyDictionary<int, int> InlineCost,
    // The estimated size of the code that creates a transient instance in place, including its transient dependencies.
    IReadOnlyDictionary<int, int> TransientCost,
    // Binding ids of the nodes that create a PerBlock instance in place, directly or through other nodes.
    HashSet<int> PerBlockDependents,
    // The ids of the overrides that the code may or may not have overridden, depending on the path to it, see MaybeOverrideCodeBuilder.
    HashSet<int> MaybeOverrides,
    // The number of the dependencies on the nodes of the cycle of a node from the nodes outside it, by binding id.
    IReadOnlyDictionary<int, int> CycleEntries,
    // Binding ids of the nodes of the cycles that have several paths through their nodes.
    HashSet<int> BranchingCycles,
    // Binding ids of the nodes whose code depends on the consumers they are injected into.
    HashSet<int> ConsumerDependents);
