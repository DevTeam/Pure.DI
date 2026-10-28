namespace Pure.DI.Core.Code;

interface IRootUseSiteCounter
{
    RootUseSiteAnalysis Analyze(DependencyGraph graph, DependencyNode root);
}

record RootUseSiteAnalysis(
    IReadOnlyDictionary<int, int> UseSiteCount,
    HashSet<int> FactoryDownstream,
    HashSet<int> OverrideConsumers,
    // The estimated size of the code that creates a node in place, including the dependencies created in place with it.
    IReadOnlyDictionary<int, int> InlineCost,
    // The estimated size of the code that creates a transient instance in place, including its transient dependencies.
    IReadOnlyDictionary<int, int> TransientCost,
    // Binding ids of the nodes that create a PerBlock instance in place, directly or through other nodes.
    HashSet<int> PerBlockDependents);
