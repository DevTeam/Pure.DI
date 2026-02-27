namespace Pure.DI.Core.Code;

/// <summary>
/// Represents a variable in the generated code.
/// </summary>
/// <param name="graph">The dependency graph.</param>
/// <param name="constructors">Constructors tools.</param>
/// <param name="Declaration">The variable declaration.</param>
/// <param name="Trace">The trace of the variable creation.</param>
record Var(
    DependencyGraph graph,
    IConstructors constructors,
    VarDeclaration Declaration,
    // ReSharper disable once NotAccessedPositionalProperty.Global
    ImmutableArray<string> Trace)
{
    private string? _codeExpression;
    private string? _baseName;

    /// <summary>
    /// Gets the type of the instance.
    /// </summary>
    public ITypeSymbol InstanceType => Declaration.InstanceType;

    /// <summary>
    /// Gets the dependency node.
    /// </summary>
    public IDependencyNode AbstractNode => Declaration.Node;

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

            // For Singleton, we need to check constructors.IsEnabled(graph) every time
            // because it can change dynamically
            if (AbstractNode.ActualLifetime == Lifetime.Singleton && constructors.IsEnabled(graph))
            {
                return Names.RootFieldName + "." + _baseName;
            }

            return _baseName;
        }
    }

    /// <summary>
    /// Gets or sets the code expression for the variable.
    /// </summary>
    public string CodeExpression
    {
        get => _codeExpression ?? Name;
        set => _codeExpression = value;
    }

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
    public bool IsCreated { get; set; } = IsCreatedDefault(Declaration.Node);

    /// <summary>
    /// Gets or sets a value indicating whether the local function was called in the current path.
    /// </summary>
    public bool IsLocalFunctionCalled { get; set; }

    /// <summary>
    /// Resets the variable to its default state, including generated code.
    /// </summary>
    /// <returns>True if the state has changed.</returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public bool ResetToDefaults()
    {
        var changed = false;
        var defaultCreated = IsCreatedDefault(Declaration.Node);
        if (defaultCreated != IsCreated)
        {
            IsCreated = defaultCreated;
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

        if (_codeExpression != null)
        {
            _codeExpression = null;
            changed = true;
        }

        if (IsLocalFunctionCalled)
        {
            IsLocalFunctionCalled = false;
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
        var defaultCreated = IsCreatedDefault(Declaration.Node);
        if (defaultCreated != IsCreated)
        {
            IsCreated = defaultCreated;
            changed = true;
        }

        // ReSharper disable once InvertIf
        if (_codeExpression != null)
        {
            _codeExpression = null;
            changed = true;
        }

        // ReSharper disable once InvertIf
        if (resetLocalFunctionCalled && IsLocalFunctionCalled)
        {
            IsLocalFunctionCalled = false;
            changed = true;
        }

        return changed;
    }

    public override string ToString() => $"{InstanceType} {Name}";

    private static bool IsCreatedDefault(IDependencyNode node) =>
        node.Arg is not null;
}
