#### Tag Unique

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/TagUniqueScenario.cs)

`Tag.Unique` is useful to register a binding with a unique tag. It will not be available through the composition root or `Resolve` methods directly, but can be injected in compositions as some kind of enumeration.


```c#
using Pure.DI;
using Shouldly;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind<IDependency<TT>>(Tag.Unique).To<AbcDependency<TT>>()
    .Bind<IDependency<TT>>(Tag.Unique).To<XyzDependency<TT>>()
    .Bind<IService<TT>>().To<Service<TT>>()

    // Composition root
    .Root<IService<string>>("Root");

var composition = new Composition();
var stringService = composition.Root;
stringService.Dependencies.Length.ShouldBe(2);

interface IDependency<T>;

class AbcDependency<T> : IDependency<T>;

class XyzDependency<T> : IDependency<T>;

interface IService<T>
{
    ImmutableArray<IDependency<T>> Dependencies { get; }
}

class Service<T>(IEnumerable<IDependency<T>> dependencies) : IService<T>
{
    public ImmutableArray<IDependency<T>> Dependencies { get; }
        = [..dependencies];
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
	ServiceᐸStringᐳ --|> IServiceᐸStringᐳ
	AbcDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : typeof(Pure.DI.UsageTests.Advanced.TagUniqueScenario.AbcDependency<Pure.DI.TT>) 
	XyzDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : typeof(Pure.DI.UsageTests.Advanced.TagUniqueScenario.XyzDependency<Pure.DI.TT>) 
	Composition ..> ServiceᐸStringᐳ : IServiceᐸStringᐳ Root
	ServiceᐸStringᐳ o-- "PerBlock" IEnumerableᐸIDependencyᐸStringᐳᐳ : IEnumerableᐸIDependencyᐸStringᐳᐳ
	IEnumerableᐸIDependencyᐸStringᐳᐳ *--  AbcDependencyᐸStringᐳ : typeof(Pure.DI.UsageTests.Advanced.TagUniqueScenario.AbcDependency<Pure.DI.TT>)  IDependencyᐸStringᐳ
	IEnumerableᐸIDependencyᐸStringᐳᐳ *--  XyzDependencyᐸStringᐳ : typeof(Pure.DI.UsageTests.Advanced.TagUniqueScenario.XyzDependency<Pure.DI.TT>)  IDependencyᐸStringᐳ
	namespace Pure.DI.UsageTests.Advanced.TagUniqueScenario {
		class AbcDependencyᐸStringᐳ {
			+AbcDependency()
		}
		class Composition {
		<<partial>>
		+IServiceᐸStringᐳ Root
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
		}
		class IDependencyᐸStringᐳ {
			<<interface>>
		}
		class IServiceᐸStringᐳ {
			<<interface>>
		}
		class ServiceᐸStringᐳ {
			+Service(IEnumerableᐸIDependencyᐸStringᐳᐳ dependencies)
		}
		class XyzDependencyᐸStringᐳ {
			+XyzDependency()
		}
	}
	namespace System.Collections.Generic {
		class IEnumerableᐸIDependencyᐸStringᐳᐳ {
				<<interface>>
		}
	}
```

