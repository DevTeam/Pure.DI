// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class Logger<T> : ILogger<T>
{
    private readonly Lazy<IObserver<LogEntry>[]> _logEntryObservers;
    
    public Logger(IObserversProvider observersProvider) => 
        _logEntryObservers = new Lazy<IObserver<LogEntry>[]>(() => observersProvider.GetObservers<LogEntry>().ToArray());

    public void Log(in LogEntry logEntry)
    {
        foreach (var logEntryObserver in _logEntryObservers.Value)
        {
            logEntryObserver.OnNext(logEntry);
        }
    }
}