#### Span and ReadOnlySpan

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/BaseClassLibrary/SpanScenario.cs)

Specifying `Span<T>` and `ReadOnlySpan<T>` work the same as with the array `T[]`.


```c#
using Pure.DI;
using Shouldly;

DI.Setup(nameof(Composition))
    .Bind<Dependency>('a').To<Dependency>()
    .Bind<Dependency>('b').To<Dependency>()
    .Bind<Dependency>('c').To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Count.ShouldBe(3);

struct Dependency;

interface IService
{
    int Count { get; }
}

class Service(ReadOnlySpan<Dependency> dependencies) : IService
{
    public int Count { get; } = dependencies.Length;
}
```

This scenario is even more efficient in the case of `Span<T>` or `ReadOnlySpan<T>` when `T` is a value type. In this case, there is no heap allocation, and the composition root `IService` looks like this:
```c#
public IService Root
{
  get
  {
    ReadOnlySpan<Dependency> dependencies = stackalloc Dependency[3] { new Dependency(), new Dependency(), new Dependency() };
    return new Service(dependencies);
  }
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
	Service *--  ReadOnlySpanᐸDependencyᐳ : ReadOnlySpanᐸDependencyᐳ
	ReadOnlySpanᐸDependencyᐳ *--  Dependency : 'a'  Dependency
	ReadOnlySpanᐸDependencyᐳ *--  Dependency : 'b'  Dependency
	ReadOnlySpanᐸDependencyᐳ *--  Dependency : 'c'  Dependency
	namespace Pure.DI.UsageTests.BCL.SpanScenario {
		class Composition {
		<<partial>>
		+IService Root
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
		}
		class Dependency {
				<<struct>>
			+Dependency()
		}
		class IService {
			<<interface>>
		}
		class Service {
			+Service(ReadOnlySpanᐸDependencyᐳ dependencies)
		}
	}
	namespace System {
		class ReadOnlySpanᐸDependencyᐳ {
				<<struct>>
		}
	}
```

