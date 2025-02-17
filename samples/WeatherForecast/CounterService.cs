// ReSharper disable ClassNeverInstantiated.Global
namespace WeatherForecast;

public class CounterService : ICounterService
{
    private long _count;

    public long Count => Interlocked.Read(ref _count);

    public long IncrementCount() =>
        Interlocked.Increment(ref _count);
}