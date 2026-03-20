// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using System.Globalization;
using static Const;

sealed class GlobalProperties : IGlobalProperties
{
    private readonly Lazy<int> _maxVariations;
    private readonly Lazy<int> _maxDependencies;
    private readonly Lazy<int> _maxMermaid;
    private readonly Lazy<string> _profilePath;
    private readonly Lazy<CultureInfo?> _culture;

    public GlobalProperties(IGeneratorOptions options)
    {
        _maxVariations = CreateLazyIntProperty(options, MaxVariationsProperty, MinVariations, DefaultVariations);
        _maxDependencies = CreateLazyIntProperty(options, MaxDependenciesProperty, MinDependencies, DefaultDependencies);
        _maxMermaid = CreateLazyIntProperty(options, MaxMermaidProperty, MinMermaid, DefaultMermaid);

        _profilePath = new Lazy<string>(() =>
            options.GlobalOptions.TryGetValue(ProfilePathProperty, out var profilePath)
                ? profilePath
                : string.Empty);

        _culture = new Lazy<CultureInfo?>(() => {
            try
            {
                return options.GlobalOptions.TryGetValue(CultureProperty, out var culture)
                    ? CultureInfo.GetCultureInfo(culture)
                    : null;
            }
            catch
            {
                // ignored
            }

            return null;
        });
    }

    public int MaxVariations => _maxVariations.Value;

    public int MaxDependencies => _maxDependencies.Value;

    public int MaxMermaid => _maxMermaid.Value;

    public CultureInfo? Culture => _culture.Value;

    public bool TryGetProfilePath(out string path) =>
        !string.IsNullOrWhiteSpace(path = _profilePath.Value);

    private static Lazy<int> CreateLazyIntProperty(IGeneratorOptions options, string propertyKey, int minValue, int defaultValue) =>
        new(() => {
            if (options.GlobalOptions.TryGetValue(propertyKey, out var valueStr)
                && !string.IsNullOrWhiteSpace(valueStr)
                && int.TryParse(valueStr, out var value)
                && value >= minValue)
            {
                return value;
            }

            return defaultValue;
        });
}