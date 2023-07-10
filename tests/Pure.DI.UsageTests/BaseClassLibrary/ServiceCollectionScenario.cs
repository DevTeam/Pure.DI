/*
$v=true
$p=7
$d=Service collection
$h=The `// OnNewRoot = On` hint specifies to create a static method that will be called for each registered composition root. This method can be used, for example, to create an _IServiceCollection_ object:
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
namespace Pure.DI.UsageTests.BCL.ServiceCollectionScenario;

using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

// {
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService
{
    IDependency Dependency { get; }
}

internal class Service : IService
{
    public Service(IDependency dependency) => 
        Dependency = dependency;

    public IDependency Dependency { get; }
}

internal partial class Composition
{
    private void Setup() =>
// }        
        // ToString = On
        // FormatCode = On
// {
        // The following hint specifies to create a static method
        // that will be called for each registered composition root:

        // OnNewRoot = On
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<IDependency>()
            .Root<IService>();

    // Creates a service collection for the current composition
    public IServiceCollection CreateServiceCollection()
    {
        var serviceCollection = new ServiceCollection();
        foreach (var (serviceType, factory) in Factories)
        {
            serviceCollection.AddTransient(serviceType, serviceProvider => factory(this)!);
        }

        return serviceCollection;
    }

    private static readonly List<(Type ServiceType, Func<Composition, object?> Factory)> Factories = new();

    // Registers the roots of the composition for use in a service collection
    private static partial void OnNewRoot<TContract, T>(
        IResolver<Composition, TContract> resolver,
        string name,
        object? tag,
        Lifetime lifetime) => 
        Factories.Add((typeof(TContract), composition => resolver.Resolve(composition)));
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
        TestTools.SaveClassDiagram(composition, nameof(ServiceCollectionScenario));
    }
}