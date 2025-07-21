#### Minimal Wep API

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/MinimalWebAPI)

This example demonstrates the creation of a Minimal Web API application in the pure DI paradigm using the Pure.DI code generator.

Composition setup file is [Composition.cs](/samples/MinimalWebAPI/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace MinimalWebAPI;

partial class Composition : ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .DependsOn(Base)

        // Owned is used here to dispose of all disposable instances associated with the root.
        .Root<Owned<Program>>(nameof(Root))
        .Root<IClockViewModel>()

        .Bind().To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself. It depends on the `Base` setup.

The web application entry point is in the [Program.cs](/samples/MinimalWebAPI/Program.cs) file:

```c#
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
```

The [project file](/samples/WebAPI/WebAPI.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.1">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.2.1" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons on Pure.DI to work with Microsoft DI |
