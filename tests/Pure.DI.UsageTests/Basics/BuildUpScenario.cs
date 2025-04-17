/*
$v=true
$p=9
$d=Build up of an existing object
$h=This example demonstrates the Build-Up pattern in dependency injection, where an existing object is injected with necessary dependencies through its properties, methods, or fields.
$f=Key Concepts:
$f=**Build-Up** - injecting dependencies into an already created object
$f=**Dependency Attribute** - marker for identifying injectable members
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.BuildUpScenario;

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
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .RootArg<string>("name")
            .Bind().To(_ => Guid.NewGuid())
            .Bind().To(ctx =>
            {
                var dependency = new Dependency();
                ctx.BuildUp(dependency);
                return dependency;
            })
            .Bind().To<Service>()

            // Composition root
            .Root<IService>("GetMyService");

        var composition = new Composition();
        var service = composition.GetMyService("Some name");
        service.Dependency.Name.ShouldBe("Some name");
        service.Dependency.Id.ShouldNotBe(Guid.Empty);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency
{
    string Name { get; }

    Guid Id { get; }
}

class Dependency : IDependency
{
    // The Dependency attribute specifies to perform an injection and its order
    [Dependency] public string Name { get; set; } = "";
    
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection and its order
    [Dependency] public void SetId(Guid id) => Id = id;
}

interface IService
{
    IDependency Dependency { get; }
}

record Service(IDependency Dependency) : IService;
// }