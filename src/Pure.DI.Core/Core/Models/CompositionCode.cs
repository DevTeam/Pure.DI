namespace Pure.DI.Core.Models;

record CompositionCode(
    DependencyGraph Source,
    Lines Code,
    in ImmutableArray<Root> PublicRoots,
    int TotalDisposablesCount,
    int DisposablesCount,
    int AsyncDisposableCount,
    int DisposablesScopedCount,
    bool IsThreadSafe,
    Lines Diagram,
    in ImmutableArray<VarDeclaration> Singletons,
    in ImmutableArray<VarDeclaration> ClassArgs,
    int MembersCount = 0)
{
    public Compilation Compilation => Source.Source.SemanticModel.Compilation;

    public bool IsLockRequired(ILocks locks) => IsThreadSafe || locks.HasLockField(Source);
}