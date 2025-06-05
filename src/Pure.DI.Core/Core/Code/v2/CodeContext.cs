namespace Pure.DI.Core.Code.v2;

record CodeContext(
    RootContext RootContext,
    Var Var,
    DeclarationPath Path,
    IVarsMap VarsMap,
    bool IsLockRequired,
    LinesBuilder Lines,
    ImmutableArray<Accumulator> Accumulators,
    HashSet<string> Overrides,
    object? ContextTag = null);