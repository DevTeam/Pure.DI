#### Bind attribute with lifetime and tag

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Attributes/BindAttributeWithLifetimeAndTagScenario.cs)


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
    [Bind(lifetime: Lifetime.Singleton, tags: ["my tag"])]
    public IDependency Dependency { get; } = new Dependency();
}

interface IService
{
    public void DoSomething();
}

class Service([Tag("my tag")] IDependency dep) : IService
{
    public void DoSomething() => dep.DoSomething();
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
	Composition ..> Service : IService Root
	Service o-- "Singleton" IDependency : "my tag"  IDependency
	IDependency o-- "Singleton" Facade : Facade
	namespace Pure.DI.UsageTests.Basics.BindAttributeWithLifetimeAndTagScenario {
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

