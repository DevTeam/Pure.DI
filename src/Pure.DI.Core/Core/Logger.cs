// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class Logger<T> : ILogger<T>
{
    private readonly Lazy<IObserver<LogEntry>[]> _logEntryObservers;
    private readonly string _source;

    public Logger(IObserversProvider observersProvider)
    {
        _source = typeof(T).Name;
        _logEntryObservers = new Lazy<IObserver<LogEntry>[]>(() => observersProvider.GetObservers<LogEntry>().ToArray());
    }

    public void Log(in LogEntry logEntry)
    {
        var curLogEntry = logEntry with { Source = _source };
        foreach (var logEntryObserver in _logEntryObservers.Value)
        {
            logEntryObserver.OnNext(curLogEntry);
        }
    }
}