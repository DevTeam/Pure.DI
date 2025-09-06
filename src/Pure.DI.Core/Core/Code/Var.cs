namespace Pure.DI.Core.Code;

record Var(VarDeclaration Declaration)
{
    private string? _codeExpression;

    public ITypeSymbol InstanceType => Declaration.InstanceType;

    public IDependencyNode AbstractNode => Declaration.Node;

    public string NameOverride { get; init; } = "";

    public string Name =>
        string.IsNullOrEmpty(NameOverride)
            ? (AbstractNode.Lifetime == Lifetime.Singleton ? $"{Names.RootFieldName}." : "") + Declaration.NameProvider.GetVariableName(AbstractNode, Declaration.PerLifetimeId)
            : NameOverride;

    public string CodeExpression
    {
        get => string.IsNullOrEmpty(_codeExpression) ? Name : _codeExpression!;
        set => _codeExpression = value;
    }

    public string LocalFunctionName { get; set; } = "";

    public Lines LocalFunction { get; set; } = new();

    public bool? HasCycle { get; set; }

    public bool IsCreated { get; set; } = IsCreatedDefault(Declaration.Node);

    public bool ResetToDefaults()
    {
        var defaultCreated = IsCreatedDefault(Declaration.Node);
        if (defaultCreated == IsCreated)
        {
            return false;
        }

        IsCreated = defaultCreated;
        return true;
    }

    public override string ToString() => $"{InstanceType} {Name}";

    private static bool IsCreatedDefault(IDependencyNode node) =>
        node.Arg is not null;
}