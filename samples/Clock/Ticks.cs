namespace Clock;

public sealed class Ticks: ITicks, IDisposable
{
    private readonly ILog<Ticks> _log;
    private readonly Timer _timer;
    private readonly bool _toBeDisposed;

    public Ticks(ILog<Ticks> log, TimeSpan period = default, Timer? timer = null)
    {
        _log = log;
        _toBeDisposed = timer == null;
        var actualPeriod = period == TimeSpan.Zero? TimeSpan.FromSeconds(1) : period;
        _timer = timer ?? new Timer(OnTick, null, TimeSpan.Zero, actualPeriod);
    }

    public event Tick? Tick;

    public void Dispose()
    {
        if (!_toBeDisposed)
        {
            return;
        }

        _timer.Dispose();
        _log.Info("Disposed");
    }

    public void OnTick(object? state = null)
    {
        _log.Info("Tick");
        Tick?.Invoke();
    }
}