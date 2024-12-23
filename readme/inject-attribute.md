#### Inject attribute

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Attributes/InjectAttributeScenario.cs)

If you want to use attributes in your libraries but don't want to create your own, you can add this package to your projects:

[![NuGet](https://img.shields.io/nuget/v/Pure.DI.Abstractions)](https://www.nuget.org/packages/Pure.DI.Abstractions)

It contains attributes like `Inject` and `Inject<T>` that work for constructors and their arguments, methods and their arguments, properties and fields. They allow you to setup all injection parameters.


```c#
using Pure.DI;
using Shouldly;
using Pure.DI.Abstractions;

DI.Setup(nameof(PersonComposition))
    .Arg<int>("personId")
    .Bind<Uri>("Person Uri").To(_ => new Uri("https://github.com/DevTeam/Pure.DI"))
    .Bind("NikName").To(_ => "Nik")
    .Bind().To<Person>()

    // Composition root
    .Root<IPerson>("Person");

var composition = new PersonComposition(personId: 123);
var person = composition.Person;
person.ToString().ShouldBe("123 Nik https://github.com/DevTeam/Pure.DI");

interface IPerson;

class Person([Inject("NikName")] string name) : IPerson
{
    private object? _state;

    [Inject<int>] internal object Id = "";

    public void Initialize([Inject<Uri>("Person Uri", 1)] object state) =>
        _state = state;

    public override string ToString() => $"{Id} {name} {_state}";
}
```

This package should also be included in a project:

[![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)


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
	Person *--  String : "NikName"  String
	Person o-- Int32 : Argument "personId"
	Person *--  Uri : "Person Uri"  Uri
	namespace Pure.DI.UsageTests.Attributes.InjectAttributeScenario {
		class IPerson {
			<<interface>>
		}
		class Person {
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
		class Int32 {
				<<struct>>
		}
		class String {
		}
		class Uri {
		}
	}
```

