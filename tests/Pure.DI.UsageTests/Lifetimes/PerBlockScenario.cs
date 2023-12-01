/*
$v=true
$p=2
$d=PerBlock
$h=_PerBlock_ lifetime is similar to _PerResolve_ lifetime. Briefly, the difference is that instances of types with _PerBlock_ lifetime will be reused in the same composition without guarantee in order to increase performance. For example, when a _PerBlock_ instance is used as a dependency by a consumer created lazily, more than one instance will be created. This lifetime is well suited for read-only instances. In this scenario, it will save memory and CPU resources.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Lifetimes.PerBlockScenario;

using Xunit;

// {
interface IDependency { }

class Dependency : IDependency { }

interface IService
{
    public IDependency Dependency1 { get; }
            
    public IDependency Dependency2 { get; }
    
    public IDependency Dependency3 { get; }
}

class Service : IService
{
    public Service(
        IDependency dependency1,
        IDependency dependency2,
        Func<IDependency> dependencyFactory)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
        Dependency3 = dependencyFactory();
    }

    public IDependency Dependency1 { get; }
            
    public IDependency Dependency2 { get; }
    
    public IDependency Dependency3 { get; }
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