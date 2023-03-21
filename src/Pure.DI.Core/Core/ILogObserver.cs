namespace Pure.DI.Core;

internal interface ILogObserver: IObserver<LogEntry>
{
    StringBuilder Log { get; }
}