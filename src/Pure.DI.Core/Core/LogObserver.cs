// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf

namespace Pure.DI.Core;

internal sealed class LogObserver(
    IBuilder<LogEntry, LogInfo> logInfoBuilder,
    IGeneratorDiagnostic diagnostic)
    : IObserver<LogEntry>
{
    private readonly HashSet<DiagnosticInfo> _diagnostics = [];

    public void OnNext(LogEntry logEntry)
    {
        var logInfo = logInfoBuilder.Build(logEntry);
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
                diagnostic.ReportDiagnostic(Diagnostic.Create(descriptor, location));
            }

            _diagnostics.Clear();
        }
    }

    private record DiagnosticInfo(
        DiagnosticDescriptor Descriptor,
        Location? Location);
}