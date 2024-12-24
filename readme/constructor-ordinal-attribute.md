#### Constructor ordinal attribute

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Attributes/ConstructorOrdinalAttributeScenario.cs)

When applied to any constructor in a type, automatic injection constructor selection is disabled. The selection will only focus on constructors marked with this attribute, in the appropriate order from smallest value to largest.


```c#
using Pure.DI;
using Shouldly;

DI.Setup(nameof(Composition))
    .Arg<string>("serviceName")
    .Bind().To<Dependency>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition(serviceName: "Xyz");
var service = composition.Root;
service.ToString().ShouldBe("Xyz");

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service : IService
{
    private readonly string _name;

    // The integer value in the argument specifies
    // the ordinal of injection
    [Ordinal(1)]
    public Service(IDependency dependency) =>
        _name = "with dependency";

    [Ordinal(0)]
    internal Service(string name) => _name = name;

    public Service() => _name = "default";

    public override string ToString() => _name;
}
```

The attribute `Ordinal` is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.


Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Service --|> IService
	Composition ..> Service : IService Root
	Service o-- String : Argument "serviceName"
	namespace Pure.DI.UsageTests.Attributes.ConstructorOrdinalAttributeScenario {
		class Composition {
		<<partial>>
		+IService Root
		}
		class IService {
			<<interface>>
		}
		class Service {
			~Service(String name)
		}
	}
	namespace System {
		class String {
		}
	}
```

