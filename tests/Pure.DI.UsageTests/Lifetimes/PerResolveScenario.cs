/*
$v=true
$p=1
$d=PerResolve
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
namespace Pure.DI.UsageTests.Lifetimes.PerResolveScenario;

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
        // ToString = On
// {            
        DI.Setup("Composition")
            .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service1 = composition.Root;
        var service2 = composition.Root;
        service1.Dependency1.ShouldBe(service1.Dependency2);
        service2.Dependency1.ShouldNotBe(service1.Dependency1);
// }
        TestTools.SaveClassDiagram(composition, nameof(PerResolveScenario));
    }
}