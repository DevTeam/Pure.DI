namespace Pure.DI.Core.Models;

internal record CompositionCode(
    DependencyGraph Source,
    LinesBuilder Code,
    in ImmutableArray<Variable> Singletons,
    in ImmutableArray<Variable> Args,
    in ImmutableArray<Root> Roots,
    int DisposablesCount,
    int DisposablesScopedCount,
    bool IsThreadSafe,
    in ImmutableArray<Line> Diagram,
    int MembersCount = 0);