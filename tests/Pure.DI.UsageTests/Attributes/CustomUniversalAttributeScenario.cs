/*
$v=true
$p=11
$d=Custom universal attribute
$h=You can use a combined attribute, and each method in the list above has an optional parameter that defines the argument number (the default is 0) from where to get the appropriate metadata for _tag_, _ordinal_, or _type_.
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
    
    [Inject<int>(ordinal: 1)]
    internal object Id = "";

    public void Initialize([Inject<Uri>] object state) => 
        _state = state;

    public override string ToString() => $"{Id} {name} {_state}";
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {            
        DI.Setup(nameof(PersonComposition))
            .TagAttribute<InjectAttribute<TT>>()
            .OrdinalAttribute<InjectAttribute<TT>>(1)
            .TypeAttribute<InjectAttribute<TT>>()
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