/*
$v=true
$p=9
$d=Builders
$h=Sometimes you need to complete an existing composition root and implement all of its dependencies, in which case the `Builder` method will be useful, as in the example below:
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.BuilderScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        DI.Setup(nameof(Composition))
            .Bind().To(_ => Guid.NewGuid())
            .Bind().To<Dependency>()
            // Service1 builder
            .Builder<Service1>("BuildUp")
            // Service2 builder
            .Builder<Service2>("BuildUp");

        var composition = new Composition();
        
        var service1 = composition.BuildUp(new Service1());
        service1.Id.ShouldNotBe(Guid.Empty);
        service1.Dependency.ShouldBeOfType<Dependency>();
        service1.Dependency.ShouldBe(service1.Dependency);
        
        var service2 = composition.BuildUp(new Service2());
        service2.Id.ShouldNotBe(Guid.Empty);
        service2.Dependency.ShouldBeOfType<Dependency>();
        service2.Dependency.ShouldNotBe(service2.Dependency);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    Guid Id { get; }
    
    IDependency? Dependency { get; }
}

record Service1: IService
{
    public Guid Id { get; private set; } = Guid.Empty;
    
    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IDependency? Dependency { get; set; }

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public void SetId(Guid id) => Id = id;
}

record Service2: IService
{
    public Guid Id { get; private set; } = Guid.Empty;

    [Dependency]
    public IDependency? Dependency => DependencyFactory?.Invoke();
    
    [Dependency]
    public Func<IDependency>? DependencyFactory { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
}
// }