/*
$v=true
$p=1
$d=PerResolve
$h=The _PerResolve_ lifetime guarantees that there will be a single instance of the dependency for each root of the composition.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Lifetimes.PerResolveScenario;

using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

class Service(
    IDependency dep1,
    IDependency dep2,
    Lazy<(IDependency dep3, IDependency dep4)> deps)
{
    public IDependency Dep1 { get; } = dep1;

    public IDependency Dep2 { get; } = dep2;

    public IDependency Dep3 { get; } = deps.Value.dep3;

    public IDependency Dep4 { get; } = deps.Value.dep4;
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>()
            .Root<Service>("Root");

        var composition = new Composition();

        var service1 = composition.Root;
        service1.Dep1.ShouldBe(service1.Dep2);
        service1.Dep3.ShouldBe(service1.Dep4);
        service1.Dep1.ShouldBe(service1.Dep3);
        
        var service2 = composition.Root;
        service2.Dep1.ShouldNotBe(service1.Dep1);
// }
        composition.SaveClassDiagram();
    }
}