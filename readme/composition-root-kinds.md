#### Composition root kinds

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/CompositionRootKindsScenario.cs)


```c#
using Pure.DI;
using static Pure.DI.RootKinds;

var composition = new Composition();
var service = composition.Root;
var otherService = composition.GetOtherService();
var dependency = Composition.Dependency;

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service : IService
{
    public Service(IDependency dependency)
    {
    }
}

class OtherService : IService;

partial class Composition
{
    void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IService>().To<Service>()
            .Bind<IService>("Other").To<OtherService>()
            .Bind<IDependency>().To<Dependency>()

            // Creates a public root method named "GetOtherService"
            .Root<IService>("GetOtherService", "Other", Public | Method)

            // Creates a private partial root method named "GetRoot"
            .Root<IService>("GetRoot", kind: Private | Partial | Method)

            // Creates a internal static root named "Dependency"
            .Root<IDependency>("Dependency", kind: Internal | Static);

    private partial IService GetRoot();

    public IService Root => GetRoot();
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
	Dependency --|> IDependency
	Service --|> IService
	OtherService --|> IService : "Other" 
	Composition ..> Dependency : IDependency Dependency
	Composition ..> Service : IService GetRoot()
	Composition ..> OtherService : IService GetOtherService()
	Service *--  Dependency : IDependency
	namespace Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario {
		class Composition {
		<<partial>>
		~IDependency Dependency
		+IService GetOtherService()
		-IService GetRoot()
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
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
		class OtherService {
			+OtherService()
		}
		class Service {
			+Service(IDependency dependency)
		}
	}
```

