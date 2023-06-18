/*
$v=true
$p=1
$d=Member Ordinal Attribute
$h=When applied to a property or field, these type members will also participate in dependency injection in the appropriate order from smallest value to largest.
$f=The attribute `Ordinal` is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
namespace Pure.DI.UsageTests.Attributes.MemberOrdinalAttributeScenario;

using System.Text;
using Shouldly;
using Xunit;

// {
internal interface IPerson
{
    string Name { get; }
}

internal class Person : IPerson
{
    private readonly StringBuilder _name = new();

    public string Name => _name.ToString();

    [Ordinal(0)]
    internal int Id;

    [Ordinal(1)]
    internal string FirstName
    {
        set
        {
            _name.Append(Id);
            _name.Append(' ');
            _name.Append(value);
        }
    }
    
    [Ordinal(2)]
    internal DateTime Birthday
    {
        set
        {
            _name.Append(' ');
            _name.Append($"{value:yyyy-MM-dd}");
        }
    }
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
            .Arg<int>("personId")
            .Arg<string>("personName")
            .Arg<DateTime>("personBirthday")
            .Bind<IPerson>().To<Person>().Root<IPerson>("Person");

        var composition = new PersonComposition(123, "Nik", new DateTime(1977, 11, 16));
        var person = composition.Person;
        person.Name.ShouldBe("123 Nik 1977-11-16");
// }            
        TestTools.SaveClassDiagram(composition, nameof(MemberOrdinalAttributeScenario));
    }
}