#### Wep application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WebApp)

This example demonstrates the creation of a Web application in the pure DI paradigm using the _Pure.DI_ code generator.

Composition setup file is [Composition.cs](/samples/WebApp/Composition.cs):

```c#
internal partial class Composition: ServiceProviderFactory<Composition>
{
    void Setup() =>
        DI.Setup(nameof(Composition))
            .DependsOn(Base)
            // Specifies not to attempt to resolve types whose fully qualified name
            // begins with Microsoft.Extensions., Microsoft.AspNetCore.
            // since ServiceProvider will be used to retrieve them.
            .Hint(
                Hint.OnCannotResolveContractTypeNameRegularExpression,
                @"^Microsoft\.(Extensions|AspNetCore)\..+$")

            .Bind().As(Singleton).To<WeatherForecastService>()
            // Provides the composition root for Home controller
            .Root<HomeController>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself. It depends on the `Base` setup.

Te web application entry point is in the [Program.cs](/samples/WebApp/Program.cs) file:

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
        <TargetFramework>net8.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.1.17">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.1.6" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                 |                                     |
|------------|-------------------------------------------------------------------------------------------------|:------------------------------------|
| Pure.DI    | [![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI Source code generator            |
| Pure.DI.MS | [![NuGet](https://buildstats.info/nuget/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Tools for working with Microsoft DI |
