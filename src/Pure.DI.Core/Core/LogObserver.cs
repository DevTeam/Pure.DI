// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf
namespace Pure.DI.Core;

internal sealed class LogObserver: IObserver<LogEntry>
{
    private readonly IBuilder<LogEntry, LogInfo> _logInfoBuilder;
    private readonly IDiagnostic _diagnostic;
    private readonly HashSet<DiagnosticInfo> _diagnostics = new();
    
    public LogObserver(
        IBuilder<LogEntry, LogInfo> logInfoBuilder,
        IDiagnostic diagnostic)
    {
        _logInfoBuilder = logInfoBuilder;
        _diagnostic = diagnostic;
    }

    public void OnNext(LogEntry logEntry)
    {
        var logInfo = _logInfoBuilder.Build(logEntry);
        if (logInfo.DiagnosticDescriptor is { } descriptor)
        {
            lock (_diagnostics)
            {
                _diagnostics.Add(new DiagnosticInfo(descriptor, logEntry.Location));
            }
        }
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
        lock (_diagnostics)
        {
            foreach (var (descriptor, location) in _diagnostics)
            {
                _diagnostic.ReportDiagnostic(Diagnostic.Create(descriptor, location));
            }

            _diagnostics.Clear();
        }
    }
    
    private record DiagnosticInfo(
        DiagnosticDescriptor Descriptor,
        Location? Location);
}