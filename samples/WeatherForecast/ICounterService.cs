namespace WeatherForecast;

public interface ICounterService
{
    long Count { get; }

    long IncrementCount();
}