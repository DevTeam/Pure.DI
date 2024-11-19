#### Custom universal attribute

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Attributes/CustomUniversalAttributeScenario.cs)

You can use a combined attribute, and each method in the list above has an optional parameter that defines the argument number (the default is 0) from where to get the appropriate metadata for _tag_, _ordinal_, or _type_.


```c#
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
```

The following partial class will be generated:

```c#
partial class PersonComposition
{
  private readonly PersonComposition _root;

  private readonly int _argPersonId;

  [OrdinalAttribute(10)]
  public PersonComposition(int personId)
  {
    _argPersonId = personId;
    _root = this;
  }

  internal PersonComposition(PersonComposition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _argPersonId = _root._argPersonId;
  }

  public IPerson Person
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Uri transientUri2 = new Uri("https://github.com/DevTeam/Pure.DI");
      string transientString1 = "Nik";
      Person transientPerson0 = new Person(transientString1);
      transientPerson0.Initialize(transientUri2);
      transientPerson0.Id = _argPersonId;
      return transientPerson0;
    }
  }
}
```

Class diagram:

```mermaid
classDiagram
	class PersonComposition {
		<<partial>>
		+IPerson Person
	}
	Person --|> IPerson
	class Person {
		+Person(String name)
		~Object Id
		+Initialize(Object state) : Void
	}
	class String
	class Int32
	Uri --|> ISpanFormattable
	Uri --|> IFormattable
	Uri --|> ISerializable
	class Uri
	class IPerson {
		<<interface>>
	}
	class ISpanFormattable {
		<<interface>>
	}
	class IFormattable {
		<<interface>>
	}
	class ISerializable {
		<<interface>>
	}
	PersonComposition ..> Person : IPerson Person
	Person *--  String : "NikName"  String
	Person o-- Int32 : Argument "personId"
	Person *--  Uri : Uri
```

