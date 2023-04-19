namespace Pure.DI.Core;

using System.Text.RegularExpressions;

internal class Filter : IFilter
{
    private readonly ILogger<Filter> _logger;
    private readonly ICache<string, Regex> _regexCache;

    public Filter(
        ILogger<Filter> logger,
        ICache<string,  Regex> regexCache)
    {
        _logger = logger;
        _regexCache = regexCache;
    }

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
            var regex = _regexCache.Get(regularExpression.Trim());
            if (!regex.IsMatch(value))
            {
                return false;
            }
        }
        catch (ArgumentException ex)
        {
            _logger.CompileError($"Invalid regular expression {regularExpression}. {ex.Message}", setup.Source.GetLocation(), LogId.ErrorInvalidMetadata);
        }

        return true;
    }
}