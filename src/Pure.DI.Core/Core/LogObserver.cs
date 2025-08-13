// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf

namespace Pure.DI.Core;

sealed class LogObserver(
    IBuilder<LogEntry, LogInfo> logInfoBuilder,
    IGeneratorDiagnostic diagnostic)
    : IObserver<LogEntry>
{
    private readonly HashSet<DiagnosticInfo> _diagnostics = [];

    public void OnNext(LogEntry logEntry)
    {
        var logInfo = logInfoBuilder.Build(logEntry);
        if (logInfo.DiagnosticDescriptor is {} descriptor)
        {
            lock (_diagnostics)
            {
                _diagnostics.Add(new DiagnosticInfo(descriptor, logEntry.Locations));
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
            foreach (var diagnosticInfo in _diagnostics)
            {
                diagnostic.ReportDiagnostic(Diagnostic.Create(diagnosticInfo.Descriptor, diagnosticInfo.Locations.FirstOrDefault(), diagnosticInfo.Locations.Skip(1)));
            }

            _diagnostics.Clear();
        }
    }


    private record DiagnosticInfo(
        DiagnosticDescriptor Descriptor,
        ImmutableArray<Location> Locations);
}