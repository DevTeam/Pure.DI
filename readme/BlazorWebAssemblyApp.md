#### Blazor WebAssembly application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/BlazorWebAssemblyApp)

[Here's an example](https://devteam.github.io/Pure.DI/) on GitHub Pages.

This example shows how to build a [Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-webassembly) application with Pure.DI, exposing generated roots through the WebAssembly host container.

> [!NOTE]
> WebAssembly uses `builder.ConfigureContainer(composition)` instead of `UseServiceProviderFactory`. Keep browser-specific services, such as `HttpClient`, registered in the normal Blazor service collection.

The composition setup file is [Composition.cs](/samples/BlazorWebAssemblyApp/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace BlazorWebAssemblyApp;

partial class Composition : ServiceProviderFactory<Composition>
{
    [System.Diagnostics.Conditional("DI")]
    private static void Setup() => DI.Setup()
        .Root<IAppViewModel>()
        .Root<IClockViewModel>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself. Only registered roots can be resolved through the Microsoft `IServiceProvider`, so each view model consumed by the host must be listed with `Root`.

The web application entry point is in the [Program.cs](/samples/BlazorWebAssemblyApp/Program.cs) file:

```c#
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Uses Composition as an alternative IServiceProviderFactory
using var composition = new Composition();
builder.ConfigureContainer(composition);
```

The [project file](/samples/BlazorWebAssemblyApp/BlazorWebAssemblyApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.3.6">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.3.6" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                               |
|------------|--------------------------------------------------------------------------------------------------|:----------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                      |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons for Pure.DI to work with Microsoft DI |
