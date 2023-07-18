namespace WebAPI.Services;

internal interface IWeatherForecastService
{
    IEnumerable<WeatherForecast> CreateWeatherForecast();
}