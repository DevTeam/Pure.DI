/*
$v=true
$p=5
$d=Inject attribute
$sa=Dependency attribute
$h=If you want attributes without defining your own, add this package:
$h=
$h=[![NuGet](https://img.shields.io/nuget/v/Pure.DI.Abstractions)](https://www.nuget.org/packages/Pure.DI.Abstractions)
$h=
$h=It provides `Inject` and `Inject<T>` for constructors, methods, properties, and fields, letting you configure injection metadata.
$h=`Inject<T>(tag, ordinal)` is the tagged form: the type argument names the contract to inject, so a member declared as `object` can still request a `Uri` with a tag. Use this package when you want injection attributes without owning a custom one, and define your own only when the name `Inject` clashes with another framework.
$f=This package should also be included in a project:
$f=
$f=[![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)
$f=The type argument is what selects the contract, so `Inject<Uri>` on an `object` member is resolved as a `Uri` and not as `object`. The declared member type still has to accept the injected value at run time.
$f=>[!NOTE]
$f=>These attributes are metadata only: the composition still owns every binding and lifetime, and Pure.DI validates the resulting graph at compile time.
$r=Shouldly;Pure.DI.Abstractions
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMember.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable RedundantNameQualifier

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Attributes.InjectAttributeScenario;

using Xunit;
using Pure.DI.Abstractions;

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
        DI.Setup(nameof(PersonComposition))
            .Arg<int>("personId")
            .Bind<Uri>("Person Uri").To(() => new Uri("https://github.com/DevTeam/Pure.DI"))
            .Bind("NikName").To(() => "Nik")
            .Bind().To<Person>()

            // Composition root
            .Root<IPerson>("Person");

        var composition = new PersonComposition(personId: 123);
        var person = composition.Person;
        person.ToString().ShouldBe("123 Nik https://github.com/DevTeam/Pure.DI");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IPerson;

class Person([Inject("NikName")] string name) : IPerson
{
    private object? _state;

    [Inject<int>] internal object Id = "";

    public void Initialize([Inject<Uri>("Person Uri", 1)] object state) =>
        _state = state;

    public override string ToString() => $"{Id} {name} {_state}";
}
// }
