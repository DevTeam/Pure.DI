/*
$v=true
$p=20
$d=Build up of an existing object
$sa=Builder
$sa=Method injection
$sa=Property injection
$h=This example shows the Build-Up pattern in dependency injection, where an existing object is injected with necessary dependencies through its properties, methods, or fields.
$h=Use it when the object is created by code you do not control and constructor injection is therefore impossible.
$f=**Build-Up** - injecting dependencies into an already created object; **Dependency Attribute** - marker for identifying injectable members.
$f=`ctx.BuildUp(instance)` inside a factory is the inline form: it fills the members of an object you just created, then you return it as the binding result.
$f=Injection follows the ordinal of `[Dependency(ordinal: n)]`; members without the attribute are ignored, so a framework can keep its own state.
$f=Nothing is disposed by the composition here: the instance was created by your factory, and its lifetime follows the binding lifetime you chose.
$f=Prefer it for third-party or framework types; for your own services, a constructor that takes the dependencies is clearer and testable.
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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .RootArg<string>("name")
            .Bind().To(Guid.NewGuid)
            .Bind().To(ctx => {
                var person = new Person();
                // Injects dependencies into an existing object
                ctx.BuildUp(person);
                return person;
            })
            .Bind().To<Greeter>()

            // Composition root
            .Root<IGreeter>("GetGreeter");

        var composition = new Composition();
        var greeter = composition.GetGreeter("Nik");

        greeter.Person.Name.ShouldBe("Nik");
        greeter.Person.Id.ShouldNotBe(Guid.Empty);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IPerson
{
    string Name { get; }

    Guid Id { get; }
}

class Person : IPerson
{
    // The Dependency attribute specifies to perform an injection and its order
    [Dependency] public string Name { get; set; } = "";

    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection and its order
    [Dependency] public void SetId(Guid id) => Id = id;
}

interface IGreeter
{
    IPerson Person { get; }
}

record Greeter(IPerson Person) : IGreeter;
// }