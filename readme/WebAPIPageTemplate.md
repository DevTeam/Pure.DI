#### Wep API

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WebAPI)

This example demonstrates the creation of a Web API application in the pure DI paradigm using the _Pure.DI_ code generator.

Composition setup is in two files:

- [Composition.cs](/samples/WebAPI/Composition.cs) - the normal part with bindings

- [Composition.ServiceProviderFactory.cs](/samples/WebAPI/Composition.ServiceProviderFactory.cs) - provides an alternative implementation of a _IServiceProviderFactory_

You can define a composition in parts. Both parts contain definitions of parts of the same _Composition_ class. 

In the first part, remember to define all the necessary roots of the composition, such as controllers:

```c#
internal partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IWeatherForecastService>().As(Singleton).To<WeatherForecastService>()
            .Root<WeatherForecastController>();
}
```

Controllers may not have binding setups, as they are the roots of the composition and are not injected anywhere. Therefore, it is sufficient only to declare them as the roots of the composition.

The second part is a bit more complicated. Its responsibility is the integration with Microsoft DI. It implements the `IServiceProviderFactory` interface:

```c#
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedTypeParameter
#pragma warning disable CS8667
namespace WebAPI;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Pure.DI;

internal partial class Composition: IServiceProviderFactory<IServiceCollection>
{
    private static readonly List<(Type ServiceType, Func<Composition, object> Factory)> Factories = new();
    private IServiceProvider _serviceProvider;

    private static void HintsSetup() =>
        DI.Setup(nameof(Composition))
            // Determines whether to generate the `OnCannotResolve<T>(...)` partial method
            // Be careful, this hint disables checks for the ability to resolve dependencies!
            .Hint(Hint.OnCannotResolve, "On")
            // Determines whether to generate a static partial method `OnNewRoot<TContract, T>(...)`
            .Hint(Hint.OnNewRoot, "On");
    
    public IServiceCollection CreateBuilder(IServiceCollection services)
    {
        // It is required for controllers to be registered as regular services.
        services.AddMvc().AddControllersAsServices();
        
        // Registers composition roots as services in the service collection
        return services.Add(Factories
            .Select(i => 
                new ServiceDescriptor(
                    i.ServiceType,
                    _ => i.Factory(this),
                    ServiceLifetime.Transient)));
    }

    public IServiceProvider CreateServiceProvider(IServiceCollection services) =>
        // Saves the service provider to use it to resolve dependencies external
        // to this composition from the service provider 
        _serviceProvider = services.BuildServiceProvider();

    // Obtaining external dependencies from the service provider
    private partial T OnCannotResolve<T>(object? tag, Lifetime lifetime) where T : notnull => 
        _serviceProvider.GetRequiredService<T>();

    // Registers the composition roots for use in a service collection
    private static partial void OnNewRoot<TContract, T>(
        IResolver<Composition, TContract> resolver,
        string name,
        object? tag,
        Lifetime lifetime) =>
        Factories.Add((typeof(TContract), composition => resolver.Resolve(composition)!));
}
```

Te web application entry point is in the [Program.cs](/samples/WebAPI/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(new Composition());
```

The [project file](/samples/WebAPI/WebAPI.csproj) looks like this:

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
        <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="7.0.7" />
        <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    </ItemGroup>
</Project>
```