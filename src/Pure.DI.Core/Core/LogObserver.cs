// ReSharper disable InvertIf
namespace Pure.DI.Core;

internal class LogObserver: ILogObserver
{
    private readonly DiagnosticSeverity _severity;
    private readonly IBuilder<LogEntry, LogInfo> _logInfoBuilder;
    private readonly IContextDiagnostic _diagnostic;

    public LogObserver(IGlobalOptions globalOptions,
        IBuilder<LogEntry, LogInfo> logInfoBuilder,
        IContextDiagnostic diagnostic)
    {
        _logInfoBuilder = logInfoBuilder;
        _diagnostic = diagnostic;
        _severity = globalOptions.Severity;
    }

    public StringBuilder Log { get; } = new();
    
    public StringBuilder Outcome { get; } = new();

    public void OnNext(LogEntry logEntry)
    {
        if (logEntry.Severity >= _severity)
        {
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
        else
        {
            if (logEntry.IsOutcome)
            {
                var logInfo = _logInfoBuilder.Build(logEntry, CancellationToken.None);
                Outcome.AppendLine(logInfo.Outcome);
            }
        }
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }
}