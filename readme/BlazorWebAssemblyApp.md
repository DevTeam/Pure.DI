#### Blazor WebAssembly application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/BlazorServerApp)

[Here's an example](https://devteam.github.io/Pure.DI/) on github.io

This example demonstrates the creation of a [Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-webassembly) application in the pure DI paradigm using the _Pure.DI_ code generator.

Composition setup file is [Composition.cs](/samples/BlazorWebAssemblyApp/Composition.cs):

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

internal partial class Composition: ServiceProviderFactory<Composition>
{
    void Setup() => DI.Setup()
        .DependsOn(Base)
        // Specifies not to attempt to resolve types whose fully qualified name
        // begins with Microsoft.Extensions., Microsoft.AspNetCore.
        // since ServiceProvider will be used to retrieve them.
        .Hint(
            Hint.OnCannotResolveContractTypeNameRegularExpression,
            @"^Microsoft\.(Extensions|AspNetCore)\..+$")
        
        // View Models
        .Bind().As(Singleton).To<ClockViewModel>()
            // Provides the composition root for Clock view model
            .Root<IClockViewModel>(nameof(ClockViewModel))

        // Services
        .Bind().To<Log<TT>>()
        .Bind().To(_ => TimeSpan.FromSeconds(1))
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

Te web application entry point is in the [Program.cs](/samples/BlazorWebAssemblyApp/Program.cs) file:

```c#
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Uses Composition as an alternative IServiceProviderFactory
using var composition = new Composition();
builder.ConfigureContainer(composition);
```

The [project file](/samples/BlazorWebAssemblyApp/BlazorWebAssemblyApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.1.41">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.1.41" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                 |                                     |
|------------|-------------------------------------------------------------------------------------------------|:------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI Source code generator            |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Tools for working with Microsoft DI |
