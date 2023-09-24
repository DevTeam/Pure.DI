// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class GlobalOptions : IGlobalOptions
{
    private readonly Lazy<int> _maxIterations;

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
    }

    public int MaxIterations => _maxIterations.Value;
}