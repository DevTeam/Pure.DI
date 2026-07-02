/*
$v=true
$p=6
$d=Custom attributes
$sa=Custom universal attribute
$h=To use custom attributes, derive from `System.Attribute` and register them with the setup API:
$h=- `TagAttribute`
$h=- `OrdinalAttribute`
$h=- `TypeAttribute`
$h=You can also use combined attributes. Each registration method can take an optional argument index (default is 0) that specifies where to read _tag_, _ordinal_, or _type_ metadata.
$f=>[!NOTE]
$f=>Custom attributes provide extensibility for advanced scenarios where standard attributes don't meet specific requirements.
$r=Shouldly
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
            .TagAttribute<MyTagAttribute>()
            .OrdinalAttribute<MyOrdinalAttribute>()
            .TypeAttribute<MyTypeAttribute>()
            .TypeAttribute<MyGenericTypeAttribute<TT>>()
            .Arg<int>("personId")
            .Bind().To(() => new Uri("https://github.com/DevTeam/Pure.DI"))
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
