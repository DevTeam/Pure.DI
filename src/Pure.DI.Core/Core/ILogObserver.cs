namespace Pure.DI.Core;

internal interface ILogObserver: IObserver<LogEntry>
{
    StringBuilder Log { get; }
    
    StringBuilder Outcome { get; }
}