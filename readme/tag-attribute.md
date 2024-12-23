#### Tag attribute

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Attributes/TagAttributeScenario.cs)

Sometimes it's important to take control of building a dependency graph. For example, when there are multiple implementations of the same contract. In this case, _tags_ will help:


```c#
using Pure.DI;
using Shouldly;

DI.Setup(nameof(Composition))
    .Bind("Abc").To<AbcDependency>()
    .Bind("Xyz").To<XyzDependency>()
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

class Dependency : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }
}

class Service(
    [Tag("Abc")] IDependency dependency1,
    [Tag("Xyz")] IDependency dependency2)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
}
```

The tag can be a constant, a type, a [smart tag](smart-tags.md), or a value of an `Enum` type. This attribute is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.


Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Service --|> IService
	AbcDependency --|> IDependency : "Abc" 
	XyzDependency --|> IDependency : "Xyz" 
	Composition ..> Service : IService Root
	Service *--  AbcDependency : "Abc"  IDependency
	Service *--  XyzDependency : "Xyz"  IDependency
	namespace Pure.DI.UsageTests.Basics.TagAttributeScenario {
		class AbcDependency {
			+AbcDependency()
		}
		class Composition {
		<<partial>>
		+IService Root
		}
		class IDependency {
			<<interface>>
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

