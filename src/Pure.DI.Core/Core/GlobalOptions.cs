namespace Pure.DI.Core;

internal class GlobalOptions : IGlobalOptions
{
    private readonly Lazy<string?> _logFile;
    private readonly Lazy<DiagnosticSeverity> _severity;
    private readonly Lazy<int> _maxIterations;

    public GlobalOptions(IContextOptions contextOptions)
    {
         _logFile = new Lazy<string?>(() => contextOptions.GlobalOptions.TryGetValue(GlobalSettings.LogFile, out var logFile) ? logFile : default);

        _severity = new Lazy<DiagnosticSeverity>(() =>
        {
            if (contextOptions.GlobalOptions.TryGetValue(GlobalSettings.Severity, out var severityStr)
                && !string.IsNullOrWhiteSpace(severityStr)
                && Enum.TryParse<DiagnosticSeverity>(severityStr, true, out var severityVal))
            {
                return severityVal;
            }

            return DiagnosticSeverity.Warning;
        });

        _maxIterations = new Lazy<int>(() =>
        {
            if (contextOptions.GlobalOptions.TryGetValue(GlobalSettings.MaxIterations, out var maxIterationsStr)
                && !string.IsNullOrWhiteSpace(maxIterationsStr)
                && int.TryParse(maxIterationsStr, out var maxIterations)
                && maxIterations >= 64)
            {
                return maxIterations;
            }

            return 1024;
        });
    }

    public string? LogFile => _logFile.Value;

    public DiagnosticSeverity Severity => _severity.Value;

    public int MaxIterations => _maxIterations.Value;
}