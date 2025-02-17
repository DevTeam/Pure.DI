// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class ObserversRegistry : IObserversRegistry, IObserversProvider
{
    private readonly Dictionary<Type, ICollection<object>> _observers = new();

    public IEnumerable<IObserver<T>> GetObservers<T>() => GetOfType<T>().Cast<IObserver<T>>();

    public IDisposable Register<T>(IObserver<T> observer)
    {
        var observers = GetOfType<T>();
        lock (observers)
        {
            observers.Add(observer);
        }

        return Disposables.Create(Remove);

        void Remove()
        {
            lock (observers)
            {
                observers.Remove(observer);
            }
        }
    }

    private ICollection<object> GetOfType<T>()
    {
        lock (_observers)
        {
            if (_observers.TryGetValue(typeof(T), out var observers))
            {
                return observers;
            }

            observers = new List<object>();
            _observers.Add(typeof(T), observers);
            return observers;
        }
    }
}