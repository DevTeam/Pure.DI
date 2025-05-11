namespace Clock;

public sealed class ClockViewModel
    : ViewModel, IAppViewModel, IClockViewModel, IDisposable
{
    private readonly ILog<ClockViewModel> _log;
    private readonly IClockModel _clockModel;
    private readonly ITicks _ticks;
    private DateTimeOffset _now;
    private string _title = "", _time = "", _date = "";

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
        get => _title;
        private set => _title = OnPropertyChanged(value);
    }

    public string Time
    {
        get => _time;
        private set => _time = OnPropertyChanged(value);
    }

    public string Date
    {
        get => _date;
        private set => _date = OnPropertyChanged(value);
    }

    [Initializable]
    public void OnTick()
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