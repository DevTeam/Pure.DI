namespace Pure.DI.Core.Code;

/// <summary>
/// A scope with a few scoped disposable instances keeps the order in which it created them
/// in one <see cref="ulong"/> field instead of an array: 4 bits per instance.
/// The root composition keeps using its array, because it also tracks singletons.
/// </summary>
interface IScopedDisposeOrder
{
    /// <summary>
    /// Determines whether scopes of the composition keep the order of their scoped disposable instances in one field.
    /// </summary>
    bool IsEnabled(DependencyGraph graph);

    /// <summary>
    /// Gets the scoped disposable fields tracked by a scope.
    /// </summary>
    IEnumerable<VarDeclaration> GetTrackedFields(CompositionCode composition);

    /// <summary>
    /// Gets the name of the constant that identifies the scoped disposable field.
    /// </summary>
    string GetIdName(VarDeclaration field);
}
