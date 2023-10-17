/*
$v=true
$p=98
$d=Service collection
$h=The `// OnNewRoot = On` hint specifies to create a static method that will be called for each registered composition root. This method can be used, for example, to create an _IServiceCollection_ object:
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedMember.Local
namespace Pure.DI.UsageTests.BCL.ServiceCollectionScenario;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Xunit;

// {
interface IDependency { }

class Dependency : IDependency { }

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(IDependency dependency) => 
        Dependency = dependency;

    public IDependency Dependency { get; }
}

partial class Composition: ServiceCollection
{
    private void Setup() =>
        // The following hint specifies to create a static method
        // that will be called for each registered composition root:

        // OnNewRoot = On
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<IDependency>()
            .Root<IService>();

    // Creates a service collection for the current composition
    public IServiceCollection CreateServiceCollection() => 
        new ServiceCollection()
            .Add(Resolvers
                .Select(i => 
                    new ServiceDescriptor(
                        i.ServiceType,
                        _ => i.Resolver.Resolve(this),
                        ServiceLifetime.Transient)));

    private static readonly List<(Type ServiceType, IResolver<Composition, object> Resolver)> Resolvers = new();

    // Registers the resolvers of the composition for use in a service collection
    private static partial void OnNewRoot<TContract, T>(
        IResolver<Composition, TContract> resolver,
        string name,
        object? tag,
        Lifetime lifetime) => 
        Resolvers.Add((typeof(TContract), (IResolver<Composition, object>)resolver));
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        var composition = new Composition();
        var serviceCollection = composition.CreateServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = (IService)serviceProvider.GetService(typeof(IService))!;
        var dependency = serviceProvider.GetService(typeof(IDependency));
        service.Dependency.ShouldBe(dependency);
// }            
        composition.SaveClassDiagram();
    }
}