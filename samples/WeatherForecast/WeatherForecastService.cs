// ReSharper disable ClassNeverInstantiated.Global
namespace WeatherForecast;

using Microsoft.Extensions.Logging;

internal class WeatherForecastService(ILogger<WeatherForecastService> logger)
    : IWeatherForecastService
{
    private static readonly string[] Summaries =
    [
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
    ];

    public async IAsyncEnumerable<WeatherForecast> CreateWeatherForecastAsync()
    {
        foreach (var index in Enumerable.Range(1, 5))
        {
            yield return await CreateWeatherForecastAsync(index);
        }
    }

    private Task<WeatherForecast> CreateWeatherForecastAsync(int index)
    {
        logger.LogInformation("Create WeatherForecast {Index}", index);
        var result = new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        };

        return Task.FromResult(result);
    }
}