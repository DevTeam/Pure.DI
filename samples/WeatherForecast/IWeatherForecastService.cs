namespace WeatherForecast;

public interface IWeatherForecastService
{
    IAsyncEnumerable<WeatherForecast> CreateWeatherForecastAsync();
}