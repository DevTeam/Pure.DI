namespace Pure.DI;

public interface IObserversRegistry
{
    IDisposable Register<T>(IObserver<T> observer);
}