/*
$v=true
$p=4
$d=Default lifetime
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
namespace Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario;

using Xunit;

// {
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService
{
    public IDependency Dependency1 { get; }
            
    public IDependency Dependency2 { get; }
}

internal class Service : IService
{
    public Service(IDependency dependency1, IDependency dependency2)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
    }

    public IDependency Dependency1 { get; }
            
    public IDependency Dependency2 { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup("Composition")
            .DefaultLifetime(Lifetime.Singleton)
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<IService>("Root");

        var composition = new Composition();
        var service1 = composition.Root;
        var service2 = composition.Root;
        service1.ShouldBe(service2);
        service1.Dependency1.ShouldBe(service1.Dependency2);
// }
    }
}