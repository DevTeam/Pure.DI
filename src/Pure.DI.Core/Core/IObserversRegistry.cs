namespace Pure.DI.Core;

internal interface IObserversRegistry
{
    IDisposable Register<T>(IObserver<T> observer);
}