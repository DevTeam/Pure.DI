namespace Pure.DI.Core;

internal class ObserversRegistry : IObserversRegistry, IObserversProvider
{
    private readonly Dictionary<Type, ICollection<object>> _observers = new();
    
    public IDisposable Register<T>(IObserver<T> observer)
    {
        var observers = GetOfType<T>();
        observers.Add(observer);

        void Remove() => observers.Remove(observer);
        return Disposables.Create(Remove);
    }

    public IEnumerable<IObserver<T>> GetObservers<T>() => GetOfType<T>().Cast<IObserver<T>>();

    private ICollection<object> GetOfType<T>()
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