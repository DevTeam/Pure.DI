/*
$v=true
$p=10
$d=Custom Attributes
$h=It's very easy to use your attributes. To do this, you need to create a descendant of the `System.Attribute` class and register it using one of the appropriate methods:
$h=- `TagAttribute`
$h=- `OrdinalAttribute`
$h=- `TagAttribute`
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.UsageTests.Attributes.CustomAttributesScenario;

using Xunit;

// {

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
internal class MyOrdinalAttribute : Attribute
{
    public MyOrdinalAttribute(int ordinal) { }
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
internal class MyTagAttribute : Attribute
{
    public MyTagAttribute(object tag) { }
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
internal class MyTypeAttribute : Attribute
{
    public MyTypeAttribute(Type type) { }
}

internal interface IPerson
{
}

internal class Person : IPerson
{
    private readonly string _name;

    public Person([MyTag("NikName")] string name) => _name = name;

    [MyOrdinal(1)]
    [MyType(typeof(int))]
    internal object Id = "";

    public override string ToString() => $"{Id} {_name}";
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
// {            
        DI.Setup("PersonComposition")
            .TagAttribute<MyTagAttribute>()
            .OrdinalAttribute<MyOrdinalAttribute>()
            .TypeAttribute<MyTypeAttribute>()
            .Arg<int>("personId")
            .Bind<string>("NikName").To(_ => "Nik")
            .Bind<IPerson>().To<Person>().Root<IPerson>("Person");

        var composition = new PersonComposition(123);
        var person = composition.Person;
        person.ToString().ShouldBe("123 Nik");
// }            
        TestTools.SaveClassDiagram(composition, nameof(CustomAttributesScenario));
    }
}