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
            // Default Lifetime applies
            // to all bindings until the end of the chain
            // or the next call to the DefaultLifetime method
            .DefaultLifetime(Lifetime.Singleton)

            .Bind().To<Dependency>()
            .RootBind<IService>("Root").To<Service>();

        var composition = new Composition();
        var service1 = composition.Root;
        var service2 = composition.Root;
        service1.ShouldBe(service2);
        service1.Dependency1.ShouldBe(service1.Dependency2);
// }
        composition.SaveClassDiagram();
    }
}