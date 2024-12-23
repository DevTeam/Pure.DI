#### Required properties or fields

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/RequiredPropertiesOrFieldsScenario.cs)

All properties or fields marked with the _required_ keyword automatically become injected dependencies.


```c#
using Pure.DI;
using Shouldly;

DI.Setup(nameof(Composition))
    .Arg<string>("name")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition(name: "My Service");
var service = composition.Root;
service.Dependency.ShouldBeOfType<Dependency>();
service.Name.ShouldBe("My Service");

interface IDependency;

class Dependency : IDependency;

interface IService
{
    string Name { get; }

    IDependency Dependency { get; }
}

class Service : IService
{
    public required string ServiceNameField;

    public string Name => ServiceNameField;

    // The required property will be injected automatically
    // without additional effort
    public required IDependency Dependency { get; init; }
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
	Service --|> IService
	Dependency --|> IDependency
	Composition ..> Service : IService Root
	Service o-- String : Argument "name"
	Service *--  Dependency : IDependency
	namespace Pure.DI.UsageTests.Basics.RequiredPropertiesOrFieldsScenario {
		class Composition {
		<<partial>>
		+IService Root
		}
		class Dependency {
			+Dependency()
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
			+Service()
			+String ServiceNameField
			+IDependency Dependency
		}
	}
	namespace System {
		class String {
		}
	}
```

