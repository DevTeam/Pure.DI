namespace Pure.DI.Core.Code;

internal record BuildContext(
    int Level,
    IBuildTools BuildTools,
    ICodeBuilder<IStatement> StatementBuilder,
    DependencyGraph DependencyGraph,
    Variable Variable,
    LinesBuilder Code,
    LinesBuilder LocalFunctionsCode,
    object? ContextTag,
    bool? LockIsRequired,
    ImmutableArray<Accumulator> Accumulators,
    bool IsFactory = false);