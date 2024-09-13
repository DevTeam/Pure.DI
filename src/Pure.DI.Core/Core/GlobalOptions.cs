// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

internal sealed class GlobalOptions : IGlobalOptions
{
    private readonly Lazy<int> _maxIterations;
    private readonly Lazy<string> _profilePath;

    public GlobalOptions(IGeneratorOptions options)
    {
        _maxIterations = new Lazy<int>(() =>
        {
            if (options.GlobalOptions.TryGetValue(GlobalSettings.MaxIterations, out var maxIterationsStr)
                && !string.IsNullOrWhiteSpace(maxIterationsStr)
                && int.TryParse(maxIterationsStr, out var maxIterations)
                && maxIterations >= 64)
            {
                return maxIterations;
            }

            return 1024;
        });

        _profilePath = new Lazy<string>(() =>
            options.GlobalOptions.TryGetValue(GlobalSettings.ProfilePath, out var profilePath)
                ? profilePath
                : string.Empty);
    }

    public int MaxIterations => _maxIterations.Value;

    public bool TryGetProfilePath(out string path) =>
        !string.IsNullOrWhiteSpace(path = _profilePath.Value);
}