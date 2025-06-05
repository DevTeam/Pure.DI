namespace Pure.DI.Core.Code.v2;

record Var(
    VarDeclaration Declaration,
    Injection Injection)
{
    public ITypeSymbol ContractType => Injection.Type;

    public ITypeSymbol InstanceType => Declaration.InstanceType;

    public DependencyNode Node => Declaration.Node;

    public string NameOverride { get; init; } = "";

    public string Name =>
        string.IsNullOrEmpty(NameOverride)
            ? (Node.Lifetime == Lifetime.Singleton ? $"{Names.RootFieldName}." : "") + Declaration.VariableNameProvider.GetVariableName(Node, Declaration.PerLifetimeId)
            : NameOverride;

    public string CodeExpression
    {
        get => Declaration.CodeExpression ?? Name;
        set => Declaration.CodeExpression = value;
    }

    public bool HasCycle { get; set; }

    public override string ToString() => $"{InstanceType.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat)} {Name}";
}