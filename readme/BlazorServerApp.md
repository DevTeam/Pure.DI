#### Blazor Server application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/BlazorServerApp)

This example demonstrates the creation of a [Blazor server](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-server) application in the pure DI paradigm using the Pure.DI code generator.

Composition setup file is [Composition.cs](/samples/BlazorServerApp/Composition.cs):

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

internal partial class Composition: ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root(...)` or `RootBind()` calls.
    void Setup() => DI.Setup()
        // Use the DI setup from the base class
        .DependsOn(Base)
        // View Models
        .Bind().To<ClockViewModel>()
            // Provides the composition root for a Clock view model
            .Root<IClockViewModel>(nameof(ClockViewModel))
        .Bind().To<ErrorViewModel>()
            // Provides the composition root for the Error view model
            .Root<IErrorViewModel>()

        // Services
        .Bind().To<Log<TT>>()
        .Bind().As(Singleton).To<Timer>()
        .Bind().As(PerBlock).To<SystemClock>()
        .Bind().As(Singleton).To<WeatherForecastService>()
            // Provides the composition root for Weather Forecast service
            .Root<IWeatherForecastService>()
        .Bind().As(Singleton).To<CounterService>()
            // Provides the composition root for Counter service
            .Root<ICounterService>()
        
        // Infrastructure
        .Bind().To<Dispatcher>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself. It depends on the `Base` setup.

The web application entry point is in the [Program.cs](/samples/BlazorServerApp/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);

// Uses Composition as an alternative IServiceProviderFactory
using var composition = new Composition();
builder.Host.UseServiceProviderFactory(composition);
```

The [project file](/samples/BlazorServerApp/BlazorServerApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.1.61">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.1.61" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons on Pure.DI to work with Microsoft DI |
