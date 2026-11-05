namespace Pure.DI.Core.Code;

/// <summary>
/// The fields of singletons are stored in an object that the root composition shares with all its scopes,
/// so a scope does not carry a field for every singleton of the composition.
/// </summary>
static class SharedSingletons
{
    public static bool IsShared(this IConstructors constructors, CompositionCode composition, VarDeclaration field) =>
        field.Node.ActualLifetime == Lifetime.Singleton && constructors.IsEnabled(composition.Source);

    public static bool HasShared(this IConstructors constructors, CompositionCode composition) =>
        composition.Singletons.Any(field => constructors.IsShared(composition, field));

    /// <summary>
    /// Gets an expression that returns the shared singletons of a composition, creating them on the first use.
    /// </summary>
    public static string GetOrCreateExpression(string owner) =>
        $"{owner}{Names.SingletonsFieldName} ?? {Names.SystemNamespace}Threading.Interlocked.CompareExchange(ref {owner}{Names.SingletonsFieldName}, new {Names.SingletonsClassName}(), null) ?? {owner}{Names.SingletonsFieldName}";
}
