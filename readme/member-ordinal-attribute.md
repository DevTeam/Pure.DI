#### Member ordinal attribute

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Attributes/MemberOrdinalAttributeScenario.cs)

When applied to a property or field, these type members will also participate in dependency injection in the appropriate order from smallest value to largest.


```c#
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

    [Ordinal(2)]
    public DateTime Birthday
    {
        set
        {
            _name.Append(' ');
            _name.Append($"{value:yyyy-MM-dd}");
        }
    }
}

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
```

The attribute `Ordinal` is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.

The following partial class will be generated:

```c#
partial class PersonComposition
{
  private readonly PersonComposition _root;

  private readonly int _argPersonId;
  private readonly string _argPersonName;
  private readonly DateTime _argPersonBirthday;

  [OrdinalAttribute(10)]
  public PersonComposition(int personId, string personName, DateTime personBirthday)
  {
    _argPersonId = personId;
    _argPersonName = personName ?? throw new ArgumentNullException(nameof(personName));
    _argPersonBirthday = personBirthday;
    _root = this;
  }

  internal PersonComposition(PersonComposition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _argPersonId = _root._argPersonId;
    _argPersonName = _root._argPersonName;
    _argPersonBirthday = _root._argPersonBirthday;
  }

  public IPerson Person
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Person transientPerson0 = new Person();
      transientPerson0.Id = _argPersonId;
      transientPerson0.FirstName = _argPersonName;
      transientPerson0.Birthday = _argPersonBirthday;
      return transientPerson0;
    }
  }
}
```

Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Person --|> IPerson
	PersonComposition ..> Person : IPerson Person
	Person o-- Int32 : Argument "personId"
	Person o-- String : Argument "personName"
	Person o-- DateTime : Argument "personBirthday"
	namespace Pure.DI.UsageTests.Attributes.MemberOrdinalAttributeScenario {
		class IPerson {
			<<interface>>
		}
		class Person {
			+Person()
			+Int32 Id
			+String FirstName
			+DateTime Birthday
		}
		class PersonComposition {
		<<partial>>
		+IPerson Person
		}
	}
	namespace System {
		class DateTime {
				<<struct>>
		}
		class Int32 {
				<<struct>>
		}
		class String {
		}
	}
```

