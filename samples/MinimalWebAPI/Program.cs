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

internal partial class Program(
    ILogger<Program> logger,
    IWeatherForecastService weatherForecast)
{
    private void Run(WebApplication app)
    {
        app.MapGet("/", async () =>
        {
            logger.LogInformation("Start of request execution");
            return await weatherForecast.CreateWeatherForecastAsync().ToListAsync();
        });

        app.Run();
    }
}