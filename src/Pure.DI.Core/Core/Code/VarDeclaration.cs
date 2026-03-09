namespace Pure.DI.Core.Code;

record VarDeclaration(
    INameProvider NameProvider,
    IVarStateTracker stateTracker,
    int PerLifetimeId,
    IDependencyNode Node)
{
    private bool _isDeclared = IsDeclaredDefault(Node);

    /// <summary>
    /// Gets or sets a value indicating whether the variable has been declared.
    /// </summary>
    public bool IsDeclared
    {
        get => _isDeclared;
        set
        {
            if (_isDeclared == value)
            {
                return;
            }

            stateTracker.OnStateChanging(Node.BindingId);
            _isDeclared = value;
        }
    }

    /// <summary>
    /// Gets the type of the instance.
    /// </summary>
    public ITypeSymbol InstanceType => Node.Node.Type;

    /// <summary>
    /// Gets the variable name.
    /// </summary>
    public string Name { get; } = NameProvider.GetVariableName(Node, PerLifetimeId);

    /// <summary>
    /// Resets the declaration to its default state.
    /// </summary>
    /// <returns>True if the state has changed.</returns>
    public bool ResetToDefaults()
    {
        var declaredDefault = IsDeclaredDefault(Node);
        if (declaredDefault == _isDeclared)
        {
            return false;
        }

        _isDeclared = declaredDefault;
        return true;
    }

    /// <summary>
    /// Resets only the mutable state of the declaration.
    /// </summary>
    /// <returns>True if the state has changed.</returns>
    public bool ResetStateToDefaults() => ResetToDefaults();

    internal void RestoreDeclaredState(bool isDeclared) => _isDeclared = isDeclared;

    public override string ToString() => $"{InstanceType} {Name}";

    private static bool IsDeclaredDefault(IDependencyNode node) =>
        node.ActualLifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve || node.Arg is not null;
}
