namespace Pure.DI.Core;

internal interface IWildcardMatcher
{
    /// <summary>
    /// Return true if the given expression matches the given name. Supports the following wildcards:
    /// '*', '?', '&lt;', '&gt;', '"'. The backslash character '\' escapes.
    /// </summary>
    /// <param name="expression">The expression to match with, such as "*.foo".</param>
    /// <param name="name">The name to check against the expression.</param>
    /// <param name="ignoreCase">True to ignore case (default).</param>
    /// <param name="useExtendedWildcards">True to use additional expressions symbols.</param>
    bool Match(
        ReadOnlySpan<char> expression,
        ReadOnlySpan<char> name,
        bool ignoreCase = false,
        bool useExtendedWildcards = false);
}