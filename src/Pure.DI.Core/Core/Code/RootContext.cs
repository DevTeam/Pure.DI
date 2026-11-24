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

    // Whether the code locks a single instance by its own lock, see #206.
    public bool InstanceLockIsInUse { get; set; }

    // The PerResolve instances whose locks the code uses.
    public HashSet<string> PerResolveLocks { get; } = new(StringComparer.Ordinal);

    public bool ReturnWasAdded { get; set; }

    public List<Var> ConstructionFailureAccumulators { get; } = [];

    public LocalFunctionScope LocalFunctionScope { get; } = new();
}
