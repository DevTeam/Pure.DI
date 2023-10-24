#### Wep API

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/BlazorServerApp)

This example demonstrates the creation of a [Blazor server](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-server) application in the pure DI paradigm using the _Pure.DI_ code generator.

Composition setup file is [Composition.cs](/samples/BlazorServerApp/Composition.cs):

```c#
internal partial class Composition: ServiceProviderFactory<Composition>
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .DependsOn(Base)
            .Bind<IWeatherForecastService>()
                .As(Singleton)
                .To<WeatherForecastService>()
                .Root<IWeatherForecastService>()
            .Bind<ICounterService>()
                .To<CounterService>()
                .Root<ICounterService>()
            .Bind<IErrorModel>()
                .To<ErrorModel>()
                .Root<IErrorModel>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself. It depends on the `Base` setup.

Te web application entry point is in the [Program.cs](/samples/BlazorServerApp/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(new Composition());
```

The [project file](/samples/BlazorServerApp/BlazorServerApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    <PropertyGroup>
        <TargetFramework>net7.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="$(version)">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="$(version)" />
    </ItemGroup>
</Project>
```

It contains 2 additional links to NuGet packages:

[![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)

[![NuGet](https://buildstats.info/nuget/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS)
