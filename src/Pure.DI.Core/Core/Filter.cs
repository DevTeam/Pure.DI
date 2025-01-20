// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

using System.Text.RegularExpressions;

internal sealed class Filter(
    ILogger logger,
    ICache<string, Regex> regexCache,
    IWildcardMatcher wildcardMatcher)
    : IFilter
{
    public static Regex RegexFactory(string filter) =>
        new(filter, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    public bool IsMeetRegularExpressions(MdSetup setup, params (Hint setting, Lazy<string> textProvider)[] settings) =>
        settings.All(i => IsMeetRegularExpression(setup, i.setting, i.textProvider));
    
    public bool IsMeetWildcards(MdSetup setup, params (Hint setting, Lazy<string> textProvider)[] settings) =>
        settings.All(i => IsMeetWildcard(setup, i.setting, i.textProvider));

    private bool IsMeetRegularExpression(
        MdSetup setup,
        Hint hint,
        Lazy<string> textProvider)
    {
        if (!setup.Hints.TryGetValue(hint, out var regularExpression)
            || string.IsNullOrWhiteSpace(regularExpression))
        {
            {
                return true;
            }
        }

        try
        {
            var regex = regexCache.Get(regularExpression.Trim(), RegexFactory);
            if (!regex.IsMatch(textProvider.Value))
            {
                return false;
            }
        }
        catch (ArgumentException ex)
        {
            logger.CompileError($"Invalid regular expression {regularExpression}. {ex.Message}", setup.Source.GetLocation(), LogId.ErrorInvalidMetadata);
        }

        return true;
    }

    private bool IsMeetWildcard(
        MdSetup setup,
        Hint hint,
        Lazy<string> textProvider)
    {
        if (!setup.Hints.TryGetValue(hint, out var wildcard)
            || string.IsNullOrWhiteSpace(wildcard))
        {
            {
                return true;
            }
        }

        try
        {
            if (!wildcardMatcher.Match(wildcard.Trim().AsSpan(), textProvider.Value.AsSpan()))
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.CompileError($"Invalid wildcard {wildcard}. {ex.Message}", setup.Source.GetLocation(), LogId.ErrorInvalidMetadata);
        }

        return true;
    }
}