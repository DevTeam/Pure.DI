// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UseCollectionExpression
namespace Pure.DI.Core;

sealed class Logger(IObserversProvider observersProvider, Type targetType) : ILogger
{
    private readonly Lazy<IObserver<LogEntry>[]> _logEntryObservers = new(() => observersProvider.GetObservers<LogEntry>().ToArray());

    public void Log(in LogEntry logEntry)
    {
        foreach (var logEntryObserver in _logEntryObservers.Value)
        {
            logEntryObserver.OnNext(logEntry with { TargetType = targetType });
        }
    }
}