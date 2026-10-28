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
    IReadOnlyDictionary<int, int> InlineCost);
