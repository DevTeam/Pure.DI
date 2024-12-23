#### Tuple

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/BaseClassLibrary/TupleScenario.cs)

The tuples feature provides concise syntax to group multiple data elements in a lightweight data structure. The following example shows how a type can ask to inject a tuple argument into it:


```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<Dependency>()
    .Bind<Point>().To(_ => new Point(7, 9))
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var root = composition.Root;

interface IDependency;

class Dependency : IDependency;

readonly record struct Point(int X, int Y);

interface IService
{
    IDependency Dependency { get; }
}

class Service((Point Point, IDependency Dependency) tuple) : IService
{
    public IDependency Dependency { get; } = tuple.Dependency;
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
	Service *--  ValueTupleᐸPointˏIDependencyᐳ : ValueTupleᐸPointˏIDependencyᐳ
	ValueTupleᐸPointˏIDependencyᐳ *--  Point : Point
	ValueTupleᐸPointˏIDependencyᐳ *--  Dependency : IDependency
	namespace Pure.DI.UsageTests.BCL.TupleScenario {
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
		class Point {
				<<struct>>
		}
		class Service {
			+Service(ValueTupleᐸPointˏIDependencyᐳ tuple)
		}
	}
	namespace System {
		class ValueTupleᐸPointˏIDependencyᐳ {
				<<struct>>
			+ValueTuple(Point item1, IDependency item2)
		}
	}
```

