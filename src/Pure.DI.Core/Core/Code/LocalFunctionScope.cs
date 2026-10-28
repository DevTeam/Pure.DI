namespace Pure.DI.Core.Code;

/// <summary>
/// The place where Ensure...Exists() local functions are declared.
/// A local function is visible to the code of its scope and is shared by all use sites in it.
/// </summary>
sealed class LocalFunctionScope
{
    private readonly Dictionary<int, string> _names = [];
    private readonly List<Lines> _functions = [];

    /// <summary>
    /// Gets the declarations of the local functions in the order they were added.
    /// </summary>
    public IEnumerable<Lines> Functions => _functions;

    /// <summary>
    /// Gets the name of the local function that creates the variable.
    /// </summary>
    public bool TryGetName(Var var, out string name) =>
        _names.TryGetValue(var.AbstractNode.BindingId, out name);

    /// <summary>
    /// Adds the local function that creates the variable.
    /// </summary>
    public void Add(Var var, string name, Lines function)
    {
        _names[var.AbstractNode.BindingId] = name;
        _functions.Add(function);
    }
}
