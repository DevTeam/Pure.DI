namespace Pure.DI.Core.Code;

interface IRootUseSiteCounter
{
    RootUseSiteAnalysis Analyze(DependencyGraph graph, DependencyNode root);
}

record RootUseSiteAnalysis(
    IReadOnlyDictionary<int, int> UseSiteCount,
    HashSet<int> FactoryDownstream,
    HashSet<int> OverrideConsumers,
    HashSet<int> AccumulatorConsumers);
