namespace Pure.DI.Core.Code;

record Var(
    DependencyGraph graph,
    IConstructors constructors,
    IVarStateTracker stateTracker,
    VarDeclaration Declaration,
    // ReSharper disable once NotAccessedPositionalProperty.Global
    ImmutableArray<string> Trace)
{
    private string? _baseName;
    private string? _rootBaseName;
    private bool _isCreated = IsCreatedDefault(Declaration.Node);
    private bool _isLocalFunctionCalled;

    /// <summary>
    /// Gets the type of the instance.
    /// </summary>
    public ITypeSymbol InstanceType => AbstractNode.Node.Type;

    /// <summary>
    /// Gets the dependency node.
    /// </summary>
    public IDependencyNode AbstractNode { get; } = Declaration.Node;

    /// <summary>
    /// Gets or sets the name override.
    /// </summary>
    public string NameOverride { get; init; } = "";

    /// <summary>
    /// Gets the variable name.
    /// </summary>
    public string Name
    {
        get
        {
            if (!string.IsNullOrEmpty(NameOverride))
            {
                return NameOverride;
            }

            // Cache the base name (without prefix) since it never changes
            _baseName ??= Declaration.NameProvider.GetVariableName(AbstractNode, Declaration.PerLifetimeId);

            // ReSharper disable once InvertIf
            if (AbstractNode.ActualLifetime is Lifetime.Singleton && constructors.IsEnabled(graph))
            {
                _rootBaseName ??= Names.RootVarName + "." + _baseName;
                return _rootBaseName;
            }

            return _baseName;
        }
    }

    /// <summary>
    /// Gets or sets the code expression for the variable.
    /// </summary>
    public string CodeExpression
    {
        get => RawCodeExpression ?? Name;
        set
        {
            if (string.Equals(RawCodeExpression, value, StringComparison.Ordinal))
            {
                return;
            }

            stateTracker.OnStateChanging(AbstractNode.BindingId);
            RawCodeExpression = value;
        }
    }

    internal string? RawCodeExpression { get; private set; }

    /// <summary>
    /// Gets or sets the name of the local function that creates the variable.
    /// </summary>
    public string LocalFunctionName { get; set; } = "";

    /// <summary>
    /// Gets or sets the lines of code for the local function.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public Lines LocalFunction { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the variable has a cyclic dependency.
    /// </summary>
    public bool? HasCycle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the variable has been created.
    /// </summary>
    public bool IsCreated
    {
        get => _isCreated;
        set
        {
            if (_isCreated == value)
            {
                return;
            }

            stateTracker.OnStateChanging(AbstractNode.BindingId);
            _isCreated = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the local function was called in the current path.
    /// </summary>
    public bool IsLocalFunctionCalled
    {
        get => _isLocalFunctionCalled;
        set
        {
            if (_isLocalFunctionCalled == value)
            {
                return;
            }

            stateTracker.OnStateChanging(AbstractNode.BindingId);
            _isLocalFunctionCalled = value;
        }
    }

    /// <summary>
    /// Resets the variable to its default state, including generated code.
    /// </summary>
    /// <returns>True if the state has changed.</returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public bool ResetToDefaults()
    {
        var changed = false;
        var defaultCreated = IsCreatedDefault(AbstractNode);
        if (defaultCreated != _isCreated)
        {
            _isCreated = defaultCreated;
            changed = true;
        }

        if (LocalFunctionName != "")
        {
            LocalFunctionName = "";
            changed = true;
        }

        if (LocalFunction.Count > 0)
        {
            LocalFunction = new Lines();
            changed = true;
        }

        if (RawCodeExpression != null)
        {
            RawCodeExpression = null;
            changed = true;
        }

        if (_isLocalFunctionCalled)
        {
            _isLocalFunctionCalled = false;
            changed = true;
        }

        // ReSharper disable once InvertIf
        if (HasCycle != null)
        {
            HasCycle = null;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Resets only the mutable state of the variable, without touching the generated code.
    /// </summary>
    /// <returns>True if the state has changed.</returns>
    public bool ResetStateToDefaults(bool resetLocalFunctionCalled)
    {
        var changed = false;
        var defaultCreated = IsCreatedDefault(AbstractNode);
        if (defaultCreated != _isCreated)
        {
            _isCreated = defaultCreated;
            changed = true;
        }

        // ReSharper disable once InvertIf
        if (RawCodeExpression != null)
        {
            RawCodeExpression = null;
            changed = true;
        }

        // ReSharper disable once InvertIf
        if (resetLocalFunctionCalled && _isLocalFunctionCalled)
        {
            _isLocalFunctionCalled = false;
            changed = true;
        }

        return changed;
    }

    internal void RestorePathState(bool isCreated, bool isLocalFunctionCalled, string? codeExpression, bool restoreLocalFunctionCalled)
    {
        _isCreated = isCreated;
        if (restoreLocalFunctionCalled)
        {
            _isLocalFunctionCalled = isLocalFunctionCalled;
        }

        RawCodeExpression = codeExpression;
    }

    public override string ToString() => $"{InstanceType} {Name}";

    private static bool IsCreatedDefault(IDependencyNode node) =>
        node.Arg is not null;
}
