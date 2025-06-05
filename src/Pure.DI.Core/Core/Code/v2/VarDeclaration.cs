namespace Pure.DI.Core.Code.v2;

record VarDeclaration(
    IVariableNameProvider VariableNameProvider,
    int PerLifetimeId,
    DependencyNode Node)
{
    private readonly Lazy<string> _name = new(() => VariableNameProvider.GetVariableName(Node, PerLifetimeId));

    public bool IsDeclared { get; set; } = DefaultIsDeclared(Node) ;

    public bool IsCreated { get; set; } = DefaultIsCreated(Node);

    public ITypeSymbol InstanceType => Node.Type;

    public string Name => _name.Value;

    public string? CodeExpression { get; set; }

    public RefKind RefKind { get; set; } = RefKind.None;

    public void Reset()
    {
        IsDeclared = DefaultIsDeclared(Node);
        IsCreated = DefaultIsCreated(Node);
    }

    private static bool DefaultIsDeclared(DependencyNode node) =>
        node.Lifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve || node.Arg is not null;

    private static bool DefaultIsCreated(DependencyNode node) =>
        node.Arg is not null;
}