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
    in ImmutableArray<SetupContextArg> SetupContextArgs,
    in ImmutableArray<SetupContextMembers> SetupContextMembers,
    in ImmutableArray<SetupContextArg> SetupContextArgsToCopy,
    in ImmutableArray<string> SetupContextMembersToCopy,
    string ScopeFactoryName,
    bool IsFactoryMethod,
    bool RequiresParentScope,
    bool IsLockRequired,
    int MembersCount = 0)
{
    public MdSetup Setup => Source.Source;

    public CompositionName Name => Setup.Name;

    public Compilation Compilation => Setup.SemanticModel.Compilation;

    public IHints Hints => Setup.Hints;
}
