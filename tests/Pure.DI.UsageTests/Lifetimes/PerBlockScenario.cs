/*
$v=true
$p=2
$d=PerBlock
$h=The _PreBlock_ lifetime does not guarantee that there will be a single instance of the dependency for each root of the composition, but is useful to reduce the number of instances of type.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Lifetimes.PerBlockScenario;

using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    public IDependency Dependency1 { get; }
            
    public IDependency Dependency2 { get; }
    
    public IDependency Dependency3 { get; }
}

class Service(
    IDependency dependency1,
    IDependency dependency2,
    Func<IDependency> dependencyFactory)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public IDependency Dependency3 { get; } = dependencyFactory();
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup("Composition")
            .Bind<IDependency>().As(Lifetime.PerBlock).To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service1 = composition.Root;
        var service2 = composition.Root;
        service1.Dependency1.ShouldBe(service1.Dependency2);
        service1.Dependency1.ShouldNotBe(service1.Dependency3);
        service2.Dependency1.ShouldNotBe(service1.Dependency1);
// }
        composition.SaveClassDiagram();
    }
}