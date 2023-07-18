// ReSharper disable ClassNeverInstantiated.Global
namespace WebAPI.Services;

internal class WeatherForecastService : IWeatherForecastService
{
    private readonly ILogger<WeatherForecastService> _logger;

    private static readonly string[] Summaries = 
    {
        "Freezing",
        "Bracing",
        "Chilly",
        "Cool",
        "Mild",
        "Warm",
        "Balmy",
        "Hot",
        "Sweltering",
        "Scorching"
    };

    public WeatherForecastService(ILogger<WeatherForecastService> logger) => 
        _logger = logger;

    public IEnumerable<WeatherForecast> CreateWeatherForecast() =>
        Enumerable
            .Range(1, 5)
            .Select(CreateWeatherForecast)
            .ToArray();

    private WeatherForecast CreateWeatherForecast(int index)
    {
        _logger.LogInformation("Create WeatherForecast {index}", index);
        return new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        };
    }
}