namespace Pure.DI.Core.Code;

internal record BuildContext(
    IBuildTools BuildTools,
    ICodeBuilder<IStatement> StatementBuilder,
    DependencyGraph DependencyGraph,
    Variable Variable,
    LinesBuilder Code,
    object? ContextTag,
    bool IsThreadSafe);