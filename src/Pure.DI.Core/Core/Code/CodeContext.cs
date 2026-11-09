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
    LocalFunctionScope LocalFunctionScope,
    bool HasOverrides = false,
    object? ContextTag = null,
    bool IsFactory = false,
    bool IsDeferred = false,
    // Per-resolve accumulators that a shared (Singleton/Scoped) instance under construction does not feed.
    ImmutableArray<Accumulator> DetachedAccumulators = default)
{
    // The overridden values that the code sees, by the name of their variable. A value is null where the code sees it overridden,
    // or the name of the parameter that tells whether the caller of a local function overrides it, see MaybeOverrideCodeBuilder.
    public Dictionary<string, string?> VisibleOverrides { get; init; } = new(StringComparer.Ordinal);

    public CodeContext CreateChild(VarInjection injection) =>
        this with { Parents = Parents.Add(VarInjection), VarInjection = injection };
}
