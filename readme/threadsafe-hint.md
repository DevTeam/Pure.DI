#### ThreadSafe hint

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Hints/ThreadSafeHintScenario.cs)

Hints are used to fine-tune code generation. The _ThreadSafe_ hint determines whether object composition will be created in a thread-safe manner. This hint is _On_ by default. It is good practice not to use threads when creating an object graph, in which case this hint can be turned off, which will lead to a slight increase in performance.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// ThreadSafe = Off`.


```c#
using static Hint;

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(Func<IDependency> dependencyFactory) : IService;

DI.Setup(nameof(Composition))
    .Hint(ThreadSafe, "Off")
    .Bind().To<Dependency>()
    .Bind().As(Lifetime.Singleton).To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
```

For more hints, see [this](README.md#setup-hints) page.

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;

  private Service? _singletonService44;

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_root._singletonService44 is null)
      {
        Func<IDependency> perBlockFunc0 = new Func<IDependency>([MethodImpl(MethodImplOptions.AggressiveInlining)] () =>
        {
          IDependency localValue92 = new Dependency();
          return localValue92;
        });
        _root._singletonService44 = new Service(perBlockFunc0);
      }

      return _root._singletonService44!;
    }
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
	Dependency --|> IDependency
	Composition ..> Service : IService Root
	Service o-- "PerBlock" FuncᐸIDependencyᐳ : FuncᐸIDependencyᐳ
	FuncᐸIDependencyᐳ *--  Dependency : IDependency
	namespace Pure.DI.UsageTests.Hints.ThreadSafeHintScenario {
		class Composition {
		<<partial>>
		+IService Root
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

