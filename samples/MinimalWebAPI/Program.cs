#pragma warning disable CS9113 // Parameter is unread.
using Microsoft.AspNetCore.Mvc;
using MinimalWebAPI;
using WeatherForecast;
var composition = new Composition();
var builder = WebApplication.CreateBuilder(args);

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);

var app = builder.Build();

// Creates an application composition root of type `Program`
var compositionRoot = composition.Root;
compositionRoot.Run(app);

partial class Program(
    // Dependencies could be injected here
    ILogger<Program> logger,
    IWeatherForecastService weatherForecast)
{
    private void Run(WebApplication app)
    {
        app.MapGet("/", async (
            // Dependencies can be injected here as well
            [FromServices] IWeatherForecastService anotherOneWeatherForecast) => {
            logger.LogInformation("Start of request execution");
            return await anotherOneWeatherForecast.CreateWeatherForecastAsync().ToListAsync();
        });

        app.Run();
    }
}