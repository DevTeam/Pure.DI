namespace Pure.DI.Core.Code;

internal record Variable(
        IStatement? Parent,
        int PerLifetimeId,
        in DependencyNode Node,
        in Injection Injection,
        ICollection<IStatement> Args,
        VariableInfo Info,
        string NameOverride = "")
        : IStatement
{
    public Variable Current => this;
    
    public bool IsDeclared { get; } = Node.Lifetime != Lifetime.Transient || Node.IsArg();
    
    public string VariableName => string.IsNullOrEmpty(NameOverride) ? Node.GetVariableName(PerLifetimeId) : NameOverride;

    public ITypeSymbol InstanceType => Node.Type;
    
    public ITypeSymbol ContractType => Injection.Type;

    public override string ToString() => $"{InstanceType} {VariableName}";
}