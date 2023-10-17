// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedTypeParameter
#pragma warning disable CS8667
namespace WebAPI;

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pure.DI;

internal partial class Composition: IServiceProviderFactory<IServiceCollection>
{
    private static readonly List<(Type ServiceType, IResolver<Composition, object> Resolver)> Resolvers = new();
    private IServiceProvider? _serviceProvider;

    [Conditional("DI")]
    private static void HintsSetup() =>
        DI.Setup(nameof(Composition))
            // Determines whether to generate the `OnCannotResolve<T>(...)` partial method
            // Be careful, this hint disables checks for the ability to resolve dependencies!
            .Hint(Hint.OnCannotResolve, "On")
            // Determines whether to generate a static partial method `OnNewRoot<TContract, T>(...)`
            .Hint(Hint.OnNewRoot, "On");
    
    public IServiceCollection CreateBuilder(IServiceCollection services) =>
        // Registers composition roots as services in the service collection
        services.Add(Resolvers
            .Select(i => 
                new ServiceDescriptor(
                    i.ServiceType,
                    _ => i.Resolver.Resolve(this),
                    ServiceLifetime.Transient)));

    public IServiceProvider CreateServiceProvider(IServiceCollection services) =>
        // Saves the service provider to use it to resolve dependencies external
        // to this composition from the service provider 
        _serviceProvider = services.BuildServiceProvider();

    // Obtaining external dependencies from the service provider
    private partial T OnCannotResolve<T>(object? tag, Lifetime lifetime) where T : notnull => 
        _serviceProvider!.GetRequiredService<T>();

    // Registers the composition resolvers for use in a service collection
    private static partial void OnNewRoot<TContract, T>(
        IResolver<Composition, TContract> resolver,
        string name,
        object? tag,
        Lifetime lifetime) => 
        Resolvers.Add((typeof(TContract), (IResolver<Composition, object>)resolver));
}