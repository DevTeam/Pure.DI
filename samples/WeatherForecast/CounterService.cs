namespace WeatherForecast;

public class CounterService : ICounterService
{
    private int _currentCount;
    
    public int IncrementCount()
    {
        _currentCount++;
        return _currentCount;
    }
}