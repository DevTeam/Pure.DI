namespace WebAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using WeatherForecast;

[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IWeatherForecastService _weatherForecastService;

    internal WeatherForecastController(
        ILogger<WeatherForecastController> logger,
        IWeatherForecastService weatherForecastService)
    {
        _logger = logger;
        _weatherForecastService = weatherForecastService;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async IAsyncEnumerable<WeatherForecast> Get()
    {
        await foreach (var forecast in _weatherForecastService.CreateWeatherForecastAsync())
        {
            _logger.LogInformation("returns {ForecastSummary}", forecast.Summary);
            yield return forecast;
        }
    }
}