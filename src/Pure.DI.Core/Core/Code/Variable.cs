namespace Pure.DI.Core.Code;

internal record Variable(
        IStatement? Parent,
        int PerLifetimeId,
        in DependencyNode Node,
        in Injection Injection,
        ICollection<IStatement> Args,
        VariableInfo Info,
        bool IsLazy,
        string NameOverride = "")
        : IStatement
{
    private string? _variableCode;
    
    public Variable Current => this;
    
    public bool IsDeclared { get; } = Node.Lifetime != Lifetime.Transient || Node.IsArg();
    
    public string VariableName => string.IsNullOrEmpty(NameOverride) ? Node.GetVariableName(PerLifetimeId) : NameOverride;

    public string VariableCode
    {
        get => _variableCode ?? VariableName;
        set => _variableCode = value;
    }

    public ITypeSymbol InstanceType => Node.Type;
    
    public ITypeSymbol ContractType => Injection.Type;

    public override string ToString() => $"{InstanceType} {VariableName}";
}