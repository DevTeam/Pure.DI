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
        // This hint indicates to not generate methods such as Resolve
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