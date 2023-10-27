namespace WeatherForecast;

internal  interface IWeatherForecastService
{
    IAsyncEnumerable<WeatherForecast> CreateWeatherForecastAsync();
}