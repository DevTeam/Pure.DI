namespace Pure.DI.Core.Code;

/// <summary>
/// Represents a map of variables used during code generation.
/// </summary>
interface IVarsMap
{
    /// <summary>
    /// Gets all variables in the map.
    /// </summary>
    IEnumerable<Var> Vars { get; }

    /// <summary>
    /// Gets all variable declarations in the map.
    /// </summary>
    IEnumerable<VarDeclaration> Declarations { get; }

    /// <summary>
    /// Gets a value indicating whether the generated code should be thread-safe.
    /// </summary>
    bool IsThreadSafe { get; }

    /// <summary>
    /// Gets a variable injection for the specified node.
    /// </summary>
    /// <param name="graph">The dependency graph.</param>
    /// <param name="injection">The injection information.</param>
    /// <param name="node">The dependency node.</param>
    /// <returns>The variable injection.</returns>
    VarInjection GetInjection(DependencyGraph graph, in Injection injection, IDependencyNode node);

    /// <summary>
    /// Resets the map to the root state.
    /// </summary>
    /// <param name="lines">The code lines for debugging.</param>
    /// <returns>A disposable that resets the state when the root is finished.</returns>
    IDisposable Root(Lines lines);

    /// <summary>
    /// Creates a scope for a local function.
    /// </summary>
    /// <param name="var">The variable representing the local function.</param>
    /// <param name="lines">The code lines for debugging.</param>
    /// <returns>A disposable that restores the state when the scope is closed.</returns>
    IDisposable LocalFunction(Var var, Lines lines);

    /// <summary>
    /// Creates a scope for a lazy initialization.
    /// </summary>
    /// <param name="var">The variable being lazily initialized.</param>
    /// <param name="lines">The code lines for debugging.</param>
    /// <returns>A disposable that restores the state when the scope is closed.</returns>
    IDisposable Lazy(Var var, Lines lines);

    /// <summary>
    /// Creates a scope for a code block.
    /// </summary>
    /// <param name="var">The variable associated with the block.</param>
    /// <param name="lines">The code lines for debugging.</param>
    /// <returns>A disposable that restores the state when the scope is closed.</returns>
    IDisposable Block(Var var, Lines lines);
}