namespace Pure.DI.Core;

internal class LogObserver: ILogObserver
{
    private readonly DiagnosticSeverity _severity = DiagnosticSeverity.Warning;
    private readonly IBuilder<LogEntry, LogInfo> _logInfoBuilder;
    private readonly IContextDiagnostic _diagnostic;

    public LogObserver(
        IContextOptions contextOptions,
        IBuilder<LogEntry, LogInfo> logInfoBuilder,
        IContextDiagnostic diagnostic)
    {
        _logInfoBuilder = logInfoBuilder;
        _diagnostic = diagnostic;
        if (contextOptions.GlobalOptions.TryGetValue(GlobalSettings.Severity, out var severityStr)
            && !string.IsNullOrWhiteSpace(severityStr)
            && Enum.TryParse<DiagnosticSeverity>(severityStr, true, out var severityVal))
        {
            _severity = severityVal;
        }
    }

    public StringBuilder Log { get; } = new();

    public void OnNext(LogEntry logEntry)
    {
        if (logEntry.Severity < _severity)
        {
            return;
        }
        
        var logInfo = _logInfoBuilder.Build(logEntry, CancellationToken.None);
        if (logInfo.DiagnosticDescriptor is { } descriptor)
        {
            _diagnostic.ReportDiagnostic(Diagnostic.Create(descriptor, logEntry.Location));            
        }

        foreach (var line in logInfo.Lines)
        {
            Log.AppendLine(line);
        }
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }
}