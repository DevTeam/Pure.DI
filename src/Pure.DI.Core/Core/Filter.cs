// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using System.Text.RegularExpressions;

internal sealed class Filter(
    ILogger<Filter> logger,
    ICache<string, Regex> regexCache)
    : IFilter
{
    public bool IsMeetRegularExpression(MdSetup setup, params (Hint setting, string value)[] settings) => 
        settings.All(i => IsMeetRegularExpression(setup, i.setting, i.value));

    private bool IsMeetRegularExpression(
        MdSetup setup,
        Hint hint,
        string value)
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
            var regex = regexCache.Get(regularExpression.Trim());
            if (!regex.IsMatch(value))
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
}