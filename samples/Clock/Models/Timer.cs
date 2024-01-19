namespace Clock.Models;

using System.Collections.Generic;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Timer : ITimer, IDisposable
{
    private readonly ILog<Timer> _log;
    private readonly System.Threading.Timer _timer;
    private readonly List<IObserver<Tick>> _observers = [];

    // ReSharper disable once MemberCanBePrivate.Global
    public Timer(ILog<Timer> log, TimeSpan period)
    {
        _log = log;
        _timer = new System.Threading.Timer(Tick, null, TimeSpan.Zero, period);
        _log.Info("Created");
    }

    public IDisposable Subscribe(IObserver<Tick> observer)
    {
        lock (_observers)
        {
            _observers.Add(observer);
        }

        _log.Info("Subscribed");
        
        return new Token(() =>
        {
            lock (observer)
            {
                _observers.Remove(observer);
            }
            
            _log.Info("Unsubscribed");
        });
    }

    public void Dispose()
    {
        _timer.Dispose();
        _log.Info("Disposed");
    }

    private void Tick(object? state)
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

    private class Token(Action action) : IDisposable
    {
        private readonly Action _action = action ?? throw new ArgumentNullException(nameof(action));

        public void Dispose() => _action();
    }
}