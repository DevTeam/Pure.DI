// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable UnusedMember.Global
namespace Pure.DI.Core;

sealed class Logger : ILogger
{

    private Logger(IObserversProvider observersProvider, Type targetType)
    {
        _observersProvider = observersProvider;
        _targetType = targetType;
        _logEntryObservers = new Lazy<IObserver<LogEntry>[]>(() => observersProvider.GetObservers<LogEntry>().ToArray());
    }

    public Logger(IObserversProvider observersProvider)
        : this(observersProvider, typeof(CodeGenerator))
    {
    }
    private readonly Lazy<IObserver<LogEntry>[]> _logEntryObservers;
    private readonly IObserversProvider _observersProvider;
    private readonly Type _targetType;

    public void Log(in LogEntry logEntry)
    {
        foreach (var logEntryObserver in _logEntryObservers.Value)
        {
            logEntryObserver.OnNext(logEntry with { TargetType = _targetType });
        }
    }

    public Logger WithTargetType(Type type) => new(_observersProvider, type);
}