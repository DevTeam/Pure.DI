#### Wep application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WebApp)

This example demonstrates the creation of a Web application in the pure DI paradigm using the _Pure.DI_ code generator.

Composition setup file is [Composition.cs](/samples/WebApp/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using Controllers;
using Microsoft.AspNetCore.Mvc;
using WeatherForecast;
using static Pure.DI.Lifetime;

internal partial class Composition: ServiceProviderFactory<Composition>
{
    void Setup() => DI.Setup()
        // Use the DI setup from the base class
        .DependsOn(Base)
        .Bind().As(Singleton).To<WeatherForecastService>()
        // Registers controllers as roots
        .Roots<Controller>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself. It depends on the `Base` setup.

The web application entry point is in the [Program.cs](/samples/WebApp/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddControllersAsServices();

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(new Composition());
```

The [project file](/samples/WebApp/WebApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.1.58">
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
