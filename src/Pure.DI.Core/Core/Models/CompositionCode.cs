namespace Pure.DI.Core.Models;

using Code.v2;

record CompositionCode(
    DependencyGraph Source,
    LinesBuilder Code,
    in ImmutableArray<Variable> Singletons,
    in ImmutableArray<Variable> Args,
    in ImmutableArray<Root> PublicRoots,
    int TotalDisposablesCount,
    int DisposablesCount,
    int AsyncDisposableCount,
    int DisposablesScopedCount,
    bool IsThreadSafe,
    in ImmutableArray<Line> Diagram,
    in ImmutableArray<VarDeclaration> Singletons2,
    in ImmutableArray<VarDeclaration> ClassArgs,
    int MembersCount = 0)
{
    public Compilation Compilation => Source.Source.SemanticModel.Compilation;
}