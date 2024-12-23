/*
$v=true
$p=10
$d=Custom attributes
$h=It's very easy to use your attributes. To do this, you need to create a descendant of the `System.Attribute` class and register it using one of the appropriate methods:
$h=- `TagAttribute`
$h=- `OrdinalAttribute`
$h=- `TagAttribute`
$h=You can also use combined attributes, and each method in the list above has an optional parameter that defines the argument number (the default is 0) from where to get the appropriate metadata for _tag_, _ordinal_, or _type_.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedTypeParameter

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Attributes.CustomAttributesScenario;

using Xunit;

// {
//# using Pure.DI;
//# using Shouldly;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {
        DI.Setup(nameof(PersonComposition))
            .TagAttribute<MyTagAttribute>()
            .OrdinalAttribute<MyOrdinalAttribute>()
            .TypeAttribute<MyTypeAttribute>()
            .TypeAttribute<MyGenericTypeAttribute<TT>>()
            .Arg<int>("personId")
            .Bind().To(_ => new Uri("https://github.com/DevTeam/Pure.DI"))
            .Bind("NikName").To(_ => "Nik")
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
[AttributeUsage(
    AttributeTargets.Constructor
    | AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Field)]
class MyOrdinalAttribute(int ordinal) : Attribute;

[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
class MyTagAttribute(object tag) : Attribute;

[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
class MyTypeAttribute(Type type) : Attribute;

[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
class MyGenericTypeAttribute<T> : Attribute;

interface IPerson;

class Person([MyTag("NikName")] string name) : IPerson
{
    private object? _state;

    [MyOrdinal(1)] [MyType(typeof(int))] internal object Id = "";

    [MyOrdinal(2)]
    public void Initialize([MyGenericType<Uri>] object state) =>
        _state = state;

    public override string ToString() => $"{Id} {name} {_state}";
}
// }