namespace Pure.DI.Core.Code;

record CodeContext(
    RootContext RootContext,
    VarInjection VarInjection,
    IVarsMap VarsMap,
    bool IsLockRequired,
    Lines Lines,
    ImmutableArray<Accumulator> Accumulators,
    HashSet<string> Overrides,
    bool HasOverrides = false,
    object? ContextTag = null,
    bool IsFactory = false);