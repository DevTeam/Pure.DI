/*
$v=true
$p=9
$d=Build up of the root
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.BuildUpRootScenario;

using Shouldly;
using Xunit;
using static Tag;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .RootArg<string>("name")
            .Bind().To(_ => Guid.NewGuid())
            .Bind().To<Dependency>()
            .RootArg<Service>("service", FromArg)
            .Bind().To<Service>(ctx =>
            {
                ctx.Inject(FromArg, out Service service);
                ctx.BuildUp(service);
                return service;
            })

            // Composition root
            .Root<IService>("BuildUp");

        var composition = new Composition();
        var service = composition.BuildUp(service: new Service(), name: "Some name");
        service.Name.ShouldBe("Some name");
        service.Id.ShouldNotBe(Guid.Empty);
        service.Dependency.ShouldBeOfType<Dependency>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    string Name { get; }

    Guid Id { get; }
    
    IDependency? Dependency { get; }
}

record Service: IService
{
    // The Ordinal attribute specifies to perform an injection and its order
    [Ordinal(1)]
    public string Name { get; set; } = "";
    
    public Guid Id { get; private set; } = Guid.Empty;
    
    [Ordinal(2)]
    public IDependency? Dependency { get; set; }

    // The Ordinal attribute specifies to perform an injection and its order
    [Ordinal(3)]
    public void SetId(Guid id) => Id = id;
}
// }