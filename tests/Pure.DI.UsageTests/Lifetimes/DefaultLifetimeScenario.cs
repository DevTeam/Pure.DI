/*
$v=true
$p=5
$d=Default lifetime
$h=For example, if some lifetime is used more often than others, you can make it the default lifetime:
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario;

using Xunit;

// {
interface IDependency { }

class Dependency : IDependency { }

interface IService
{
    public IDependency Dependency1 { get; }
            
    public IDependency Dependency2 { get; }
}

class Service : IService
{
    public Service(
        IDependency dependency1,
        IDependency dependency2)
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
        // ToString = On
        // FormatCode = On
// {            
        DI.Setup("Composition")
            .DefaultLifetime(Lifetime.Singleton)
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service1 = composition.Root;
        var service2 = composition.Root;
        service1.ShouldBe(service2);
        service1.Dependency1.ShouldBe(service1.Dependency2);
// }
        TestTools.SaveClassDiagram(composition, nameof(DefaultLifetimeScenario));
    }
}