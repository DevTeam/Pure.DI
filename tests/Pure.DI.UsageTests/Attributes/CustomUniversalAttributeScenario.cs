/*
$v=true
$p=7
$d=Custom universal attribute
$sa=Custom attributes
$h=A combined attribute can supply _tag_, _ordinal_, and _type_ metadata. Each registration method can take an optional argument index (default is 0) that specifies where to read the metadata.
$f=>[!NOTE]
$f=>Universal attributes reduce the number of attributes needed by combining multiple metadata types into a single attribute.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMember.Global

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Attributes.CustomUniversalAttributeScenario;

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
            .TagAttribute<InjectAttribute<TT>>()
            .OrdinalAttribute<InjectAttribute<TT>>(1)
            .TypeAttribute<InjectAttribute<TT>>()
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
    | AttributeTargets.Method
    | AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
class InjectAttribute<T>(object? tag = null, int ordinal = 0) : Attribute;

interface IPerson;

class Person([Inject<string>("NikName")] string name) : IPerson
{
    private object? _state;

    [Inject<int>(ordinal: 1)] internal object Id = "";

    public void Initialize([Inject<Uri>] object state) =>
        _state = state;

    public override string ToString() => $"{Id} {name} {_state}";
}
// }
