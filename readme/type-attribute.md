#### Type attribute

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Attributes/TypeAttributeScenario.cs)

The injection type can be defined manually using the `Type` attribute. This attribute explicitly overrides an injected type, otherwise it would be determined automatically based on the type of the constructor/method, property, or field parameter.


```c#
using Pure.DI;
using Shouldly;

DI.Setup(nameof(Composition))
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<AbcDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }
}

class Service(
    [Type(typeof(AbcDependency))] IDependency dependency1,
    [Type(typeof(XyzDependency))] IDependency dependency2)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
}
```

This attribute is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.


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
	Service *--  AbcDependency : AbcDependency
	Service *--  XyzDependency : XyzDependency
	namespace Pure.DI.UsageTests.Attributes.TypeAttributeScenario {
		class AbcDependency {
			+AbcDependency()
		}
		class Composition {
		<<partial>>
		+IService Root
		}
		class IService {
			<<interface>>
		}
		class Service {
			+Service(IDependency dependency1, IDependency dependency2)
		}
		class XyzDependency {
			+XyzDependency()
		}
	}
```

