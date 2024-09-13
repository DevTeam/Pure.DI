#### Custom attributes

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Attributes/CustomAttributesScenario.cs)

It's very easy to use your attributes. To do this, you need to create a descendant of the `System.Attribute` class and register it using one of the appropriate methods:
- `TagAttribute`
- `OrdinalAttribute`
- `TagAttribute`
You can also use combined attributes, and each method in the list above has an optional parameter that defines the argument number (the default is 0) from where to get the appropriate metadata for _tag_, _ordinal_, or _type_.


```c#
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
      transientPerson0.Id = _argPersonId;
      transientPerson0.Initialize(transientUri2);
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
	class Int32
	Uri --|> ISpanFormattable
	Uri --|> IFormattable
	Uri --|> ISerializable
	class Uri
	class String
	Person --|> IPerson
	class Person {
		+Person(String name)
		~Object Id
		+Initialize(Object state) : Void
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
	class IPerson {
		<<interface>>
	}
	PersonComposition ..> Person : IPerson Person
	Person *--  String : "NikName"  String
	Person o-- Int32 : Argument "personId"
	Person *--  Uri : Uri
```

