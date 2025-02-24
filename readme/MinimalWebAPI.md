#### Minimal Wep API

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/MinimalWebAPI)

This example demonstrates the creation of a Minimal Web API application in the pure DI paradigm using the _Pure.DI_ code generator.

Composition setup file is [Composition.cs](/samples/MinimalWebAPI/Composition.cs):

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

internal partial class Composition: ServiceProviderFactory<Composition>
{
    void Setup() => DI.Setup()
        // Use the DI setup from the base class
        .DependsOn(Base)
        .Bind().As(Singleton).To<WeatherForecastService>()
            .Root<IWeatherForecastService>()

        // Application composition root
        .Root<Program>("Root");
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself. It depends on the `Base` setup.

The web application entry point is in the [Program.cs](/samples/MinimalWebAPI/Program.cs) file:

```c#
var composition = new Composition();
var builder = WebApplication.CreateBuilder(args);

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);

var app = builder.Build();

// Creates an application composition root of type `Program`
var compositionRoot = composition.Root;
compositionRoot.Run(app);

internal partial class Program(
    // Dependencies could be injected here
    ILogger<Program> logger,
    IWeatherForecastService weatherForecast)
{
    private void Run(WebApplication app)
    {
        app.MapGet("/", async (
            // Dependencies can be injected here as well
            [FromServices] IWeatherForecastService anotherOneWeatherForecast) =>
        {
            logger.LogInformation("Start of request execution");
            return await anotherOneWeatherForecast.CreateWeatherForecastAsync().ToListAsync();
        });

        app.Run();
    }
}
```

The [project file](/samples/WebAPI/WebAPI.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.1.57">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.1.57" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons on Pure.DI to work with Microsoft DI |
