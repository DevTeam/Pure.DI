// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class GlobalProperties : IGlobalProperties
{
    public const string SeverityProperty = "build_property.purediseverity";
    public const string LogFileProperty = "build_property.puredilogfile";
    private const string MaxIterationsProperty = "build_property.puredimaxiterations";
    private const string ProfilePathProperty = "build_property.purediprofilepath";

    public GlobalProperties(IGeneratorOptions options)
    {
        _maxIterations = new Lazy<int>(() => {
            if (options.GlobalOptions.TryGetValue(MaxIterationsProperty, out var maxIterationsStr)
                && !string.IsNullOrWhiteSpace(maxIterationsStr)
                && int.TryParse(maxIterationsStr, out var maxIterations)
                && maxIterations >= 64)
            {
                return maxIterations;
            }

            return 1024;
        });

        _profilePath = new Lazy<string>(() =>
            options.GlobalOptions.TryGetValue(ProfilePathProperty, out var profilePath)
                ? profilePath
                : string.Empty);
    }

    private readonly Lazy<int> _maxIterations;
    private readonly Lazy<string> _profilePath;

    public int MaxIterations => _maxIterations.Value;

    public bool TryGetProfilePath(out string path) =>
        !string.IsNullOrWhiteSpace(path = _profilePath.Value);
}