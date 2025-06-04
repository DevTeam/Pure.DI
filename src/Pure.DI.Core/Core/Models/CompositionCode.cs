namespace Pure.DI.Core.Models;

record CompositionCode(
    DependencyGraph Source,
    LinesBuilder Code,
    in ImmutableArray<Root> PublicRoots,
    int TotalDisposablesCount,
    int DisposablesCount,
    int AsyncDisposableCount,
    int DisposablesScopedCount,
    bool IsThreadSafe,
    in ImmutableArray<Line> Diagram,
    in ImmutableArray<VarDeclaration> Singletons,
    in ImmutableArray<VarDeclaration> ClassArgs,
    int MembersCount = 0)
{
    public Compilation Compilation => Source.Source.SemanticModel.Compilation;
}