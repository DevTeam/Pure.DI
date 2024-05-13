/*
$v=true
$p=0
$d=Singleton
$h=The _Singleton_ lifetime ensures that there will be a single instance of the dependency for each composition.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Lifetimes.SingletonScenario;

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
            .Bind().As(Lifetime.Singleton).To<Dependency>()
            .RootBind<IService>("Root").To<Service>();

        var composition = new Composition();
        var service1 = composition.Root;
        var service2 = composition.Root;
        service1.Dependency1.ShouldBe(service1.Dependency2);
        service2.Dependency1.ShouldBe(service1.Dependency1);
// }
        composition.SaveClassDiagram();
    }
}