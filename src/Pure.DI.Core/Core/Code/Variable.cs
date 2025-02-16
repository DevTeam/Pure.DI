namespace Pure.DI.Core.Code;

internal record Variable(
    IVariableNameProvider VariableNameProvider,
    MdSetup Setup,
    IStatement? Parent,
    int PerLifetimeId,
    in DependencyNode Node,
    in Injection Injection,
    ICollection<IStatement> Args,
    VariableInfo Info,
    bool IsLazy,
    bool HasCycle,
    string NameOverride = "",
    RefKind RefKind = RefKind.None)
    : IStatement
{
    private string? _variableCode;

    public Variable Current => this;

    public Block ParentBlock => this.GetPath().OfType<Block>().First();

    public bool IsDeclared { get; set; } = Node.Lifetime is not Lifetime.Transient and not Lifetime.PerBlock || Node.Arg is not null;

    public bool HasCycledReference { get; set; }

    public string VariableDeclarationName =>
        string.IsNullOrEmpty(NameOverride)
            ? VariableNameProvider.GetVariableName(Node, PerLifetimeId)
            : NameOverride;

    public string VariableName => string.IsNullOrEmpty(NameOverride)
        ? (Node.Lifetime == Lifetime.Singleton ? $"{Names.RootFieldName}." : "") + VariableNameProvider.GetVariableName(Node, PerLifetimeId)
        : NameOverride;

    public string VariableCode
    {
        get => _variableCode ?? VariableName;
        set => _variableCode = value;
    }

    public ITypeSymbol InstanceType => Node.Type;

    public ITypeSymbol ContractType => Injection.Type;

    public override string ToString() => $"{InstanceType.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat)} {VariableName}";
}