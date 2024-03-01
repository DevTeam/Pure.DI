#### Blazor Server application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/BlazorServerApp)

This example demonstrates the creation of a [Blazor server](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-server) application in the pure DI paradigm using the _Pure.DI_ code generator.

Composition setup file is [Composition.cs](/samples/BlazorServerApp/Composition.cs):

```c#
internal partial class Composition: ServiceProviderFactory<Composition>
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .DependsOn(Base)
            // Specifies not to attempt to resolve types whose fully qualified name
            // begins with Microsoft.Extensions., Microsoft.AspNetCore.
            // since ServiceProvider will be used to retrieve them.
            .Hint(
                Hint.OnCannotResolveContractTypeNameRegularExpression,
                "^Microsoft\\.(Extensions|AspNetCore)\\..+$")
            
            // View Models
            .Bind<IClockViewModel>()
                .To<ClockViewModel>()
                .Root<IClockViewModel>("ClockViewModel")
            .Bind<IErrorViewModel>()
                .To<ErrorViewModel>()
                .Root<IErrorViewModel>()

            // Services
            .Bind<ILog<TT>>().To<Log<TT>>()
            .Bind<TimeSpan>().To(_ => TimeSpan.FromSeconds(1))
            .Bind<ITimer>().As(Singleton).To<Timer>()
            .Bind<IClock>().As(PerBlock).To<SystemClock>()
            .Bind<IWeatherForecastService>()
                .As(Singleton)
                .To<WeatherForecastService>()
                .Root<IWeatherForecastService>()
            .Bind<ICounterService>()
                .As(Singleton)
                .To<CounterService>()
                .Root<ICounterService>()
            
            // Infrastructure
            .Bind<IDispatcher>().To<Dispatcher>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself. It depends on the `Base` setup.

Te web application entry point is in the [Program.cs](/samples/BlazorServerApp/Program.cs) file:

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
        <TargetFramework>net8.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.1.3">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.1.3" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                 |                                     |
|------------|-------------------------------------------------------------------------------------------------|:------------------------------------|
| Pure.DI    | [![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI Source code generator            |
| Pure.DI.MS | [![NuGet](https://buildstats.info/nuget/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Tools for working with Microsoft DI |
