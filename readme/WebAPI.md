#### Wep API

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WebAPI)

This example demonstrates the creation of a Web API application in the pure DI paradigm using the _Pure.DI_ code generator.

Composition setup is in two files:

- [Composition.cs](/samples/WebAPI/Composition.cs) - the normal part with bindings

- [WebComposition.cs](/samples/WebAPI/WebComposition.cs) - the setup that provides a collection of Microsoft DI services

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

The second part is a bit more complicated. Its responsibility is the integration with Microsoft DI. This is where the roots of the composition are registered in the Microsoft DI service collection:

```c#
internal partial class Composition
{
    private static readonly List<(Type ServiceType, Func<Composition, object?> Factory)> Factories = new();
    private readonly WebApplicationBuilder _builder;
    private IServiceProvider _serviceProvider;
    
    private static void HintsSetup() =>
        DI.Setup(nameof(Composition))
            // Determines whether to generate the `OnCannotResolve<T>(...)` partial method
            // Be careful, this hint disables checks for the ability to resolve dependencies!
            .Hint(Hint.OnCannotResolve, "On")
            // Determines whether to generate a static partial method `OnNewRoot<TContract, T>(...)`
            .Hint(Hint.OnNewRoot, "On");

    public Composition(WebApplicationBuilder builder): this()
    {
        _builder = builder;
        // Registers composition roots as services in the service collection
        foreach (var (serviceType, factory) in Factories)
        {
            // Uses the Transient lifetime, since the actual lifetime is controlled in this class
            // and not in the service provider
            builder.Services.AddTransient(serviceType, _ => factory(this)!);
        }
    }
    
    public WebApplication BuildWebApplication()
    {
        var webApplication = _builder.Build();
        // Saves the service provider to use it to resolve dependencies external to this composition
        // from the service provider 
        _serviceProvider = webApplication.Services;
        return webApplication;
    }

    // Obtaining external dependencies from the service provider
    private partial T OnCannotResolve<T>(object? tag, Lifetime lifetime) => 
        _serviceProvider.GetService<T>()
        ?? throw new InvalidOperationException($"Cannot resolve {tag} {typeof(T)} from the service provider.");

    // Registers the composition roots for use in a service collection
    private static partial void OnNewRoot<TContract, T>(IResolver<Composition, TContract> resolver, string name, object? tag, Lifetime lifetime) =>
        Factories.Add((typeof(TContract), composition => resolver.Resolve(composition)));
}
```

The web application entry point is in the [Program.cs](/samples/WebAPI/Program.cs) file:

```c#
var composition = new Composition(builder);
builder.Services.AddMvc().AddControllersAsServices();
....
var app = composition.BuildWebApplication();
```

Note the absence of the `AddControllersAsServices()` call. It is required for controllers to be registered as regular services.  An object of type Composition creates a web application because it additionally needs to store a service provider in its field to resolve external dependencies.

The [project file](/samples/WebAPI/WebAPI.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    <PropertyGroup>
        <TargetFramework>net7.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.0.20">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="7.0.7" />
        <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    </ItemGroup>
</Project>
```