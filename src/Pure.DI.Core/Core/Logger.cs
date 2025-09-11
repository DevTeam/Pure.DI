// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
namespace Pure.DI.Core;

sealed class Logger : ILogger
{
    private readonly Lazy<IObserver<LogEntry>[]> _logEntryObservers;
    private readonly Type _targetType;

    public Logger(IObserversProvider observersProvider, Type targetType)
    {
        _targetType = targetType;
        _logEntryObservers = new Lazy<IObserver<LogEntry>[]>(() => observersProvider.GetObservers<LogEntry>().ToArray());
    }

    public void Log(in LogEntry logEntry)
    {
        foreach (var logEntryObserver in _logEntryObservers.Value)
        {
            logEntryObserver.OnNext(logEntry with { TargetType = _targetType });
        }
    }
}