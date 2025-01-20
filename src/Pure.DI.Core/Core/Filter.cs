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

    public bool IsMeet(
        MdSetup setup,
        params (Hint regularExpressionSetting, Hint wildcardSettings, Func<string> textProvider)[] settings) =>
        settings.All(i => IsMeet(setup, i.regularExpressionSetting, i.wildcardSettings, i.textProvider));

    private bool IsMeet(
        MdSetup setup,
        Hint regularExpressionSetting,
        Hint wildcardSettings,
        Func<string> textProvider)
    {
        var hasRegularExpressions = setup.Hints.TryGetValue(regularExpressionSetting, out var regularExpressions) && regularExpressions.Count > 0;
        var hasWildcards = setup.Hints.TryGetValue(wildcardSettings, out var wildcards) && wildcards.Count > 0;
        
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (!hasRegularExpressions && !hasWildcards)
        {
            return true;
        }

        var text = textProvider().Trim();
        if (hasRegularExpressions)
        {
            foreach (var regularExpression in regularExpressions!.Where(regularExpression => !string.IsNullOrWhiteSpace(regularExpression)))
            {
                try
                {
                    var regex = regexCache.Get(regularExpression.Trim(), RegexFactory);
                    if (regex.IsMatch(text))
                    {
                        return true;
                    }
                }
                catch (ArgumentException ex)
                {
                    logger.CompileError($"Invalid regular expression {regularExpression}. {ex.Message}", setup.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                }
            }
        }

        // ReSharper disable once InvertIf
        if (hasWildcards)
        {
            foreach (var wildcard in wildcards!.Where(wildcard => !string.IsNullOrWhiteSpace(wildcard)))
            {
                try
                {
                    if (wildcardMatcher.Match(wildcard.Trim().AsSpan(), text.AsSpan()))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    logger.CompileError($"Invalid wildcard {wildcard}. {ex.Message}", setup.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                }
            }
        }

        return false;
    }
}