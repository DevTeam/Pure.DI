namespace Clock;

public sealed class ClockViewModel
    : ViewModel, IAppViewModel, IClockViewModel, IDisposable
{
    private readonly ILog<ClockViewModel> _log;
    private readonly IClockModel _clockModel;
    private readonly ITicks _ticks;
    private DateTimeOffset _now;

    public ClockViewModel(
        ILog<ClockViewModel> log,
        IClockModel clockModel,
        ITicks ticks)
    {
        _log = log;
        _clockModel = clockModel;
        _ticks = ticks;
        ticks.Tick += OnTick;
    }

    public string Title
    {
        get;
        private set => field = OnPropertyChanged(value);
    } = "";

    public string Time
    {
        get;
        private set => field = OnPropertyChanged(value);
    } = "";

    public string Date
    {
        get;
        private set => field = OnPropertyChanged(value);
    } = "";

    private void OnTick()
    {
        _now = _clockModel.Now;
        _log.Info($"Updating by {_now}");
        Title = $"Pure.DI Clock {_now:f}";
        Date = $"{_now:d}";
        Time = $"{_now:T}";
    }

    public void Dispose()
    {
        _ticks.Tick -= OnTick;
        _log.Info("Disposed");
    }
}