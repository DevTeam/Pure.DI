#### Bind attribute with lifetime and tag

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Attributes/BindAttributeWithLifetimeAndTagScenario.cs)


```c#
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

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Facade>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.DoSomething();
```

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly Lock _lock;

  private IDependency? _singletonIDependency0;
  private Facade? _singletonFacade43;

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
    _lock = new Lock();
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = _root._lock;
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_root._singletonIDependency0 is null)
      {
        using (_lock.EnterScope())
        {
          if (_root._singletonIDependency0 is null)
          {
            if (_root._singletonFacade43 is null)
            {
              _root._singletonFacade43 = new Facade();
            }

            Facade localInstance_1182D12746 = _root._singletonFacade43!;
            _root._singletonIDependency0 = localInstance_1182D12746.Dependency;
          }
        }
      }

      return new Service(_root._singletonIDependency0!);
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

