using MinimalWebAPI;

using var composition = new Composition();
var builder = WebApplication.CreateBuilder(args);

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);

var app = builder.Build();

// Creates an application composition root of type `Owned<Program>`
using var root = composition.Root;
root.Value.Run(app);

partial class Program(
    IClockViewModel clock,
    IAppViewModel appModel)
{
    private void Run(WebApplication app)
    {
        app.MapGet("/", (
            // Dependencies can be injected here as well
            [FromServices] ILogger<Program> logger) => {
            logger.LogInformation("Start of request execution");
            return new ClockResult(appModel.Title, clock.Date, clock.Time);
        });

        app.Run();
    }
}