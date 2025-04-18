namespace Clock.Models;

using System.Collections.Generic;

// ReSharper disable once ClassNeverInstantiated.Global
class Timer : ITimer, IDisposable
{
    private readonly ILog<Timer> _log;
    private readonly List<IObserver<Tick>> _observers = [];
    private readonly System.Threading.Timer _timer;

    // ReSharper disable once MemberCanBePrivate.Global
    public Timer(ILog<Timer> log, TimeSpan period)
    {
        _log = log;
        _timer = new System.Threading.Timer(Tick, null, TimeSpan.Zero, period);
        _log.Info("Created");
    }

    public void Dispose()
    {
        _timer.Dispose();
        _log.Info("Disposed");
    }

    public IDisposable Subscribe(IObserver<Tick> observer)
    {
        lock (_observers)
        {
            _observers.Add(observer);
        }

        _log.Info("Subscribed");
        return new Token(this, observer);
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
            observer.OnNext(default);
        }
    }

    private class Token(Timer timer, IObserver<Tick> observer) : IDisposable
    {
        public void Dispose()
        {
            lock (observer)
            {
                timer._observers.Remove(observer);
            }

            timer._log.Info("Unsubscribed");
        }
    }
}