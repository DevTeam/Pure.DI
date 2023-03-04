namespace Pure.DI.Core;

internal interface IObserversProvider
{
    IEnumerable<IObserver<T>> GetObservers<T>();
}