// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable UnusedMember.Global
namespace Pure.DI.Core;

internal sealed class Logger : ILogger
{
    private readonly Lazy<IObserver<LogEntry>[]> _logEntryObservers;
    private readonly Type _targetType;
    private readonly IObserversProvider _observersProvider;

    private Logger(IObserversProvider observersProvider, Type targetType)
    {
        _observersProvider = observersProvider;
        _targetType = targetType;
        _logEntryObservers = new Lazy<IObserver<LogEntry>[]>(() => observersProvider.GetObservers<LogEntry>().ToArray());
    }
    
    public Logger(IObserversProvider observersProvider)
        : this(observersProvider, typeof(Generator))
    {
    }

    public Logger WithTargetType(Type type) => new(_observersProvider, type);

    public void Log(in LogEntry logEntry)
    {
        foreach (var logEntryObserver in _logEntryObservers.Value)
        {
            logEntryObserver.OnNext(logEntry with {TargetType = _targetType});
        }
    }
}