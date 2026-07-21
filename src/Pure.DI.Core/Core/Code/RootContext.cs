namespace Pure.DI.Core.Code;

record RootContext(
    DependencyGraph Graph,
    Root Root,
    IVarsMap VarsMap,
    Lines Lines,
    RootUseSiteAnalysis UseSites)
{
    public bool IsThreadSafeEnabled => Graph.Source.Hints.IsThreadSafeEnabled;

    public bool LockIsInUse { get; set; }

    public bool ReturnWasAdded { get; set; }

    public List<Var> ConstructionFailureAccumulators { get; } = [];
}
