namespace Pure.DI.Core.Code;

record VarDeclaration(
    INameProvider NameProvider,
    int PerLifetimeId,
    IDependencyNode Node)
{
    private readonly Lazy<string> _name = new(() => NameProvider.GetVariableName(Node, PerLifetimeId));

    public bool IsDeclared { get; set; } = IsDeclaredDefault(Node) ;

    public ITypeSymbol InstanceType => Node.Node.Type;

    public string Name => _name.Value;

    public bool ResetToDefaults()
    {
        var declaredDefault = IsDeclaredDefault(Node);
        if (declaredDefault == IsDeclared)
        {
            return false;
        }

        IsDeclared = declaredDefault;
        return true;
    }

    public override string ToString() => $"{InstanceType} {Name}";

    private static bool IsDeclaredDefault(IDependencyNode node) =>
        node.Lifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve || node.Arg is not null;
}