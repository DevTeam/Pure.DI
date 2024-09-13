// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

internal sealed class Logger<T>(IObserversProvider observersProvider) : ILogger<T>
{
    private readonly Lazy<IObserver<LogEntry>[]> _logEntryObservers = new(() => observersProvider.GetObservers<LogEntry>().ToArray());

    public void Log(in LogEntry logEntry)
    {
        foreach (var logEntryObserver in _logEntryObservers.Value)
        {
            logEntryObserver.OnNext(logEntry);
        }
    }
}