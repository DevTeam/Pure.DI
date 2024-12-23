#### Bind attribute

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Attributes/BindAttributeScenario.cs)

`BindAttribute` allows you to perform automatic binding to properties, fields or methods that belong to the type of the binding involved.


```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Facade>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.DoSomething();

interface IDependency
{
    public void DoSomething();
}

class Dependency : IDependency
{
    public void DoSomething()
    {
    }
}

class Facade
{
    [Bind]
    public IDependency Dependency { get; } = new Dependency();
}

interface IService
{
    public void DoSomething();
}

class Service(IDependency dep) : IService
{
    public void DoSomething() => dep.DoSomething();
}
```

This attribute `BindAttribute` applies to field properties and methods, to regular, static, and even returning generalized types.


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
	Service *--  IDependency : IDependency
	IDependency o-- "Singleton" Facade : Facade
	namespace Pure.DI.UsageTests.Basics.BindAttributeScenario {
		class Composition {
		<<partial>>
		+IService Root
		}
		class Facade {
			+Facade()
		}
		class IDependency {
				<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
			+Service(IDependency dep)
		}
	}
```

