/*
$v=true
$p=6
$d=Default lifetime for a type
$h=For example, if a certain lifetime is used more often than others, you can make it the default lifetime for a certain type:
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Lifetimes.DefaultLifetimeForTypeScenario;

using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

class Service(
    IDependency dependency1,
    IDependency dependency2)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup(nameof(Composition))
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            // Default lifetime applied to a specific type
            .DefaultLifetime<IDependency>(Lifetime.Singleton)
            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<IService>("Root");

        var composition = new Composition();
        var service1 = composition.Root;
        var service2 = composition.Root;
        service1.ShouldNotBe(service2);
        service1.Dependency1.ShouldBe(service1.Dependency2);
        service1.Dependency1.ShouldBe(service2.Dependency1);
// }
        composition.SaveClassDiagram();
    }
}