namespace WebApp.Models;

using WeatherForecast;

public class IndexViewModel(
    IWeatherForecastService weatherForecastService,
    string title = "Welcome",
    string aboutUrl = "https://github.com/DevTeam/Pure.DI/blob/master/readme/WebApp.md")
{
    public string Title { get; } = title;
    
    public string AboutUrl { get; } = aboutUrl;

    public IAsyncEnumerable<WeatherForecast> WeatherForecast =>
        weatherForecastService.CreateWeatherForecastAsync();
}