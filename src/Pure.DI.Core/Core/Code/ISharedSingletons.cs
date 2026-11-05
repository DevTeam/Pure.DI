namespace Pure.DI.Core.Code;

/// <summary>
/// The fields of singletons are stored in an object that the root composition shares with all its scopes,
/// so a scope does not carry a field for every singleton of the composition.
/// </summary>
interface ISharedSingletons
{
    /// <summary>
    /// Determines whether the field is stored in the object shared by the root composition and its scopes.
    /// </summary>
    bool IsShared(CompositionCode composition, VarDeclaration field);

    /// <summary>
    /// Determines whether the composition has fields stored in the shared object.
    /// </summary>
    bool HasShared(CompositionCode composition);

    /// <summary>
    /// Gets an expression that returns the shared singletons of a composition, creating them on the first use.
    /// </summary>
    /// <param name="owner">The prefix of the composition that owns the shared singletons, for example <c>parentScope.</c>.</param>
    string GetOrCreateExpression(string owner);
}
