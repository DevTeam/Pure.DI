#### Custom universal attribute

You can use a combined attribute, and each method in the list above has an optional parameter that defines the argument number (the default is 0) from where to get the appropriate metadata for _tag_, _ordinal_, or _type_.


```c#
using Shouldly;
using Pure.DI;

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
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later is installed
```bash
dotnet --list-sdk
```
- Create a net9.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to NuGet packages
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
  - [Shouldly](https://www.nuget.org/packages/Shouldly)
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

The following partial class will be generated:

```c#
partial class PersonComposition
{
  private readonly PersonComposition _root;

  private readonly int _argPersonId;

  [OrdinalAttribute(128)]
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
      string transientString1 = "Nik";
      Uri transientUri2 = new Uri("https://github.com/DevTeam/Pure.DI");
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
---
 config:
  maxTextSize: 2147483647
  maxEdges: 2147483647
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Uri --|> IFormattable
	Uri --|> ISpanFormattable
	Uri --|> IEquatableᐸUriᐳ
	Uri --|> ISerializable
	Person --|> IPerson
	PersonComposition ..> Person : IPerson Person
	Person o-- Int32 : Argument "personId"
	Person *--  Uri : Uri
	Person *--  String : "NikName"  String
	namespace Pure.DI.UsageTests.Attributes.CustomUniversalAttributeScenario {
		class IPerson {
			<<interface>>
		}
		class Person {
				<<class>>
			+Person(String name)
			~Object Id
			+Initialize(Object state) : Void
		}
		class PersonComposition {
		<<partial>>
		+IPerson Person
		}
	}
	namespace System {
		class IEquatableᐸUriᐳ {
			<<interface>>
		}
		class IFormattable {
			<<interface>>
		}
		class Int32 {
				<<struct>>
		}
		class ISpanFormattable {
			<<interface>>
		}
		class String {
				<<class>>
		}
		class Uri {
				<<class>>
		}
	}
	namespace System.Runtime.Serialization {
		class ISerializable {
			<<interface>>
		}
	}
```

