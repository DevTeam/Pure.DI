/*
$v=true
$p=1
$d=Member ordinal attribute
$sa=Constructor ordinal attribute
$sa=Dependency attribute
$h=When applied to a field, property, method, or method parameter, the member participates in DI, ordered by ordinal (ascending).
$f=The `Ordinal` attribute is part of the API, but you can define your own in any assembly or namespace.
$f=For an injection method, `Ordinal` can be placed on the method or its parameters. A method-level ordinal takes precedence; otherwise, the lowest parameter ordinal determines the method's execution order.
$f=Required fields and required or selected `init` properties are assigned in the object initializer before regular member injection. Within regular member injection, lower ordinals run first; equal ordinals are ordered by field, property, and method, then by declaration order within each kind.
$f=For equal ordinals and the same member kind, members declared on a derived type run before members inherited from its base types. Equal ordinals are valid and do not produce a diagnostic. Negative ordinal values are also supported.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable RedundantArgumentDefaultValue

namespace Pure.DI.UsageTests.Attributes.MemberOrdinalAttributeScenario;

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

    // The Ordinal attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Ordinal(0)] public int Id;

    [Ordinal(1)]
    public string FirstName
    {
        set
        {
            _name.Append(Id);
            _name.Append(' ');
            _name.Append(value);
        }
    }

    public void SetBirthday([Ordinal(2)] DateTime value)
    {
        _name.Append(' ');
        _name.Append($"{value:yyyy-MM-dd}");
    }
}
// }
