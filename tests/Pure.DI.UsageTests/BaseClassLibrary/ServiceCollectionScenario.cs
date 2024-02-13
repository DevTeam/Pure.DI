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
using MS;
using Shouldly;
using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service([Tag("Dependency Key")] IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition: ServiceProviderFactory<Composition>
{
    public IServiceCollection ServiceCollection =>
        CreateServiceCollection(this);

    private void Setup() =>
        DI.Setup(nameof(Composition))
            .DependsOn(Base)
            .Bind<IDependency>("Dependency Key").As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<IDependency>(tag: "Dependency Key")
            .Root<IService>();
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        var composition = new Composition();
        var serviceCollection = composition.ServiceCollection;
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IService>();
        var dependency = serviceProvider.GetRequiredKeyedService<IDependency>("Dependency Key");
        service.Dependency.ShouldBe(dependency);
// }            
        composition.SaveClassDiagram();
    }
}