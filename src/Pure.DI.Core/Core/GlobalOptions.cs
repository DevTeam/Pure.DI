namespace Pure.DI.Core;

internal class GlobalOptions : IGlobalOptions
{
    private readonly Lazy<string?> _logFile;
    private readonly Lazy<DiagnosticSeverity> _severity;

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
    }

    public string? LogFile => _logFile.Value;

    public DiagnosticSeverity Severity => _severity.Value;
}