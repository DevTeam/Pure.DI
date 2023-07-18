namespace WebAPI;

using Pure.DI;

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