/*
$v=true
$p=2
$d=Dependency attribute
$sa=Member ordinal attribute
$sa=Inject attribute
$h=When applied to a property or field, the member participates in DI, ordered by ordinal (ascending). Members are set in that order, so a dependency injected earlier is already available to one injected later. This is how a type whose constructor stays untouched still receives dependencies.
$f=Member injection fits objects that must keep a parameterless constructor or that are created outside the graph and then built up. Here the order is observable: `Id` is assigned before `FirstName`, and `FirstName` before `Birthday`, which is what produces the expected name.
$f=An injected field must be writable and an injected property must have a setter; a get-only member is not injected at all. Because the order decides what a setter can rely on, give distinct ordinals wherever the sequence matters.
$f=The `Dependency` attribute is part of the API, but you can define your own in any assembly or namespace.
$f=>[!NOTE]
$f=>Prefer constructor injection for required dependencies. Member injection suits optional members and framework-created objects, and these same attributes are what make the [build-up](build-up-of-an-existing-object.md) pattern work.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Attributes.DependencyAttributeScenario;

using System.Text;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Text;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(PersonComposition))
            .Arg<int>("personId")
            .Arg<string>("personName")
            .Arg<DateTime>("personBirthday")
            .Bind().To<Person>()

            // Composition root
            .Root<IPerson>("Person");

        var composition = new PersonComposition(
            personId: 123,
            personName: "Nik",
            personBirthday: new DateTime(1977, 11, 16));

        var person = composition.Person;
        person.Name.ShouldBe("123 Nik 1977-11-16");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IPerson
{
    string Name { get; }
}

class Person : IPerson
{
    private readonly StringBuilder _name = new();

    public string Name => _name.ToString();

    [Dependency] public int Id;

    // The Ordinal attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Dependency(ordinal: 1)] 
    public string FirstName
    {
        set
        {
            _name.Append(Id);
            _name.Append(' ');
            _name.Append(value);
        }
    }

    [Dependency(ordinal: 2)] 
    public DateTime Birthday
    {
        set
        {
            _name.Append(' ');
            _name.Append($"{value:yyyy-MM-dd}");
        }
    }
}
// }
