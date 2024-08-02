/*
$v=true
$p=14
$d=Bind attribute with lifetime and tag
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Basics.BindAttributeWithLifetimeAndTagScenario;

using Xunit;

// {
interface IDependency
{
    public void DoSomething();
}

class Dependency : IDependency
{
    public void DoSomething() { }
}

class Facade
{
    [Bind(lifetime: Lifetime.Singleton, tags: ["my tag"])]
    public IDependency Dependency { get; }= new Dependency();
}

interface IService
{
    public void DoSomething();
}

class Service([Tag("my tag")] IDependency dep) : IService
{
    public void DoSomething() => dep.DoSomething();
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind().As(Lifetime.Singleton).To<Facade>()
            .Bind().To<Service>()
            
            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.DoSomething();
// }
        composition.SaveClassDiagram();
    }
}