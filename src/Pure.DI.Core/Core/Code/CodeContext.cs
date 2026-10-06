namespace Pure.DI.Core.Code;

record CodeContext(
    RootContext RootContext,
    in ImmutableArray<VarInjection> Parents,
    VarInjection VarInjection,
    IVarsMap VarsMap,
    bool IsLockRequired,
    Lines Lines,
    in ImmutableArray<Accumulator> Accumulators,
    HashSet<string> Overrides,
    bool HasOverrides = false,
    object? ContextTag = null,
    bool IsFactory = false,
    bool IsDeferred = false,
    // Per-resolve accumulators that a shared (Singleton/Scoped) instance under construction does not feed.
    ImmutableArray<Accumulator> DetachedAccumulators = default)
{
    public CodeContext CreateChild(VarInjection injection) =>
        this with { Parents = Parents.Add(VarInjection), VarInjection = injection };
}
