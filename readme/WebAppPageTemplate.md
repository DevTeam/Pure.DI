#### Web application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WebApp)

This example shows how to build an ASP.NET Core MVC web application with Pure.DI, registering controllers as generated roots while keeping compatibility with the Microsoft service-provider pipeline.

> [!TIP]
> Controllers must be registered both as ASP.NET Core services and as Pure.DI roots. The sample uses `.Roots<ControllerBase>()` in the composition and `AddControllersAsServices()` in `Program.cs`.

The composition setup file is [Composition.cs](/samples/WebApp/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace WebApp;

partial class Composition : ServiceProviderFactory<Composition>
{
    [System.Diagnostics.Conditional("DI")]
    private static void Setup() => DI.Setup()
        .Roots<ControllerBase>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself. Only registered roots can be resolved through the Microsoft `IServiceProvider`, which is why controllers are registered with `.Roots<ControllerBase>()`.

The web application entry point is in the [Program.cs](/samples/WebApp/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddControllersAsServices();

using var composition = new Composition();

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);
```

The [project file](/samples/WebApp/WebApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="$(version)">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="$(ms.version)" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                               |
|------------|--------------------------------------------------------------------------------------------------|:----------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                      |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons for Pure.DI to work with Microsoft DI |
