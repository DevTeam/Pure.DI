namespace Pure.DI.Core;

interface IWildcardMatcher
{
    /// <summary>
    ///     Return true if the given expression matches the given name. Supports the following wildcards:
    ///     '*', '?', '&lt;', '&gt;', '"'. The backslash character '\' escapes.
    /// </summary>
    /// <param name="wildcard">The wildcard expression to match with, such as "*.foo".</param>
    /// <param name="text">The text to check against the expression.</param>
    /// <param name="ignoreCase">True to ignore case (default).</param>
    /// <param name="useExtendedWildcards">True to use additional expressions symbols.</param>
    bool Match(
        ReadOnlySpan<char> wildcard,
        ReadOnlySpan<char> text,
        bool ignoreCase = false,
        bool useExtendedWildcards = false);
}