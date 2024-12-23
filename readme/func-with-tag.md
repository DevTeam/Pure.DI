#### Func with tag

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/BaseClassLibrary/FuncWithTagScenario.cs)


```c#
using Pure.DI;
using Shouldly;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind<IDependency>("my tag").To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Length.ShouldBe(3);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service([Tag("my tag")] Func<IDependency> dependencyFactory): IService
{
    public ImmutableArray<IDependency> Dependencies { get; } =
    [
        dependencyFactory(),
        dependencyFactory(),
        dependencyFactory()
    ];
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
	Dependency --|> IDependency : "my tag" 
	Composition ..> Service : IService Root
	Service o-- "PerBlock" FuncᐸIDependencyᐳ : "my tag"  FuncᐸIDependencyᐳ
	FuncᐸIDependencyᐳ *--  Dependency : "my tag"  IDependency
	namespace Pure.DI.UsageTests.BCL.FuncWithTagScenario {
		class Composition {
		<<partial>>
		+IService Root
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
		class Service {
			+Service(FuncᐸIDependencyᐳ dependencyFactory)
		}
	}
	namespace System {
		class FuncᐸIDependencyᐳ {
				<<delegate>>
		}
	}
```

