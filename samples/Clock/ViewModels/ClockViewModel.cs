namespace Clock.ViewModels;

using Models;

// ReSharper disable once ClassNeverInstantiated.Global
class ClockViewModel : ViewModel, IClockViewModel, IDisposable, IObserver<Tick>
{

    public ClockViewModel(
        ILog<ClockViewModel> log,
        IClock clock,
        ITimer timer)
    {
        _log = log;
        _clock = clock;
        _now = _clock.Now;
        _timerToken = timer.Subscribe(this);
        log.Info("Created");
    }
    private readonly IClock _clock;
    private readonly ILog<ClockViewModel> _log;
    private readonly IDisposable _timerToken;
    private DateTimeOffset _now;

    public string Time => _now.ToString("T");

    public string Date => _now.ToString("d");

    void IDisposable.Dispose()
    {
        _timerToken.Dispose();
        _log.Info("Disposed");
    }

    void IObserver<Tick>.OnNext(Tick value)
    {
        _now = _clock.Now;
        _log.Info("Tick");
        OnPropertyChanged(nameof(Time));
        OnPropertyChanged(nameof(Date));
    }

    void IObserver<Tick>.OnError(Exception error) {}

    void IObserver<Tick>.OnCompleted() {}
}