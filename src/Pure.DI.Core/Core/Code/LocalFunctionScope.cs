namespace Pure.DI.Core.Code;

/// <summary>
/// The place where Ensure...Exists() local functions are declared.
/// A local function is visible to the code of its scope and is shared by all use sites in it.
/// </summary>
sealed class LocalFunctionScope
{
    private readonly Dictionary<string, string> _names = [];
    private readonly List<Lines> _functions = [];
    private readonly HashSet<string> _visibleAccumulators = [];

    /// <summary>
    /// Creates a scope.
    /// </summary>
    /// <param name="visibleAccumulators">The accumulators declared before the place of this scope.</param>
    public LocalFunctionScope(IEnumerable<Accumulator>? visibleAccumulators = null)
    {
        if (visibleAccumulators is not null)
        {
            AddVisibleAccumulators(visibleAccumulators);
        }
    }

    /// <summary>
    /// Gets the declarations of the local functions in the order they were added.
    /// </summary>
    public IEnumerable<Lines> Functions => _functions;

    /// <summary>
    /// Gets the name of a local function by its key.
    /// </summary>
    public bool TryGetName(string key, out string name) =>
        _names.TryGetValue(key, out name);

    /// <summary>
    /// Adds a local function.
    /// </summary>
    public void Add(string key, string name, Lines function)
    {
        _names[key] = name;
        _functions.Add(function);
    }

    /// <summary>
    /// Marks the accumulators as declared before the place of this scope.
    /// </summary>
    public void AddVisibleAccumulators(IEnumerable<Accumulator> accumulators)
    {
        foreach (var accumulator in accumulators)
        {
            _visibleAccumulators.Add(accumulator.VarInjection.Var.Name);
        }
    }

    /// <summary>
    /// Determines whether a local function of this scope can refer to the accumulator.
    /// </summary>
    public bool CanSee(Accumulator accumulator) =>
        _visibleAccumulators.Contains(accumulator.VarInjection.Var.Name);
}
