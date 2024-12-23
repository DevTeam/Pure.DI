#### A few partial classes

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/SeveralPartialClassesScenario.cs)

The setting code for one Composition can be located in several methods and/or in several partial classes.


```c#
using Pure.DI;

var composition = new Composition();
var service = composition.Root;

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

partial class Composition
{
    // This method will not be called in runtime
    static void Setup1() =>
        DI.Setup()
            .Bind<IDependency>().To<Dependency>();
}

partial class Composition
{
    // This method will not be called in runtime
    static void Setup2() =>
        DI.Setup()
            .Bind<IService>().To<Service>();
}

partial class Composition
{
    // This method will not be called in runtime
    private static void Setup3() =>
        DI.Setup()
            .Root<IService>("Root");
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
	Service *--  Dependency : IDependency
	namespace Pure.DI.UsageTests.Advanced.SeveralPartialClassesScenario {
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
			+Service(IDependency dependency)
		}
	}
```

