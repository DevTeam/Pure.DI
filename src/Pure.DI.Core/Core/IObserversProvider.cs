namespace Pure.DI.Core;

interface IObserversProvider
{
    IEnumerable<IObserver<T>> GetObservers<T>();
}