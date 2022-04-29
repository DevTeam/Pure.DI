namespace Clock.Models;

using System.Collections.Generic;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Timer : ITimer, IDisposable
{
    private readonly System.Threading.Timer _timer;
    private readonly List<IObserver<Tick>> _observers = new();

    // ReSharper disable once MemberCanBePrivate.Global
    public Timer(TimeSpan period) => _timer = new System.Threading.Timer(Tick, null, TimeSpan.Zero, period);

    public IDisposable Subscribe(IObserver<Tick> observer)
    {
        lock (_observers)
        {
            _observers.Add(observer);
        }

        return new Token(() =>
        {
            lock (observer)
            {
                _observers.Remove(observer);
            }
        });
    }

    public void Dispose() => _timer.Dispose();

    private void Tick(object state)
    {
        IObserver<Tick>[] observers;
        lock (_observers)
        {
            observers = _observers.ToArray();
        }

        foreach (var observer in observers)
        {
            observer.OnNext(Models.Tick.Shared);
        }
    }

    private class Token : IDisposable
    {
        private readonly Action _action;

        public Token(Action action) => _action = action ?? throw new ArgumentNullException(nameof(action));

        public void Dispose() => _action();
    }
}