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