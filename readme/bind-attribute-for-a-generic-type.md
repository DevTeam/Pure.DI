#### Bind attribute for a generic type

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Attributes/BindAttributeForGenericTypeScenario.cs)


```c#
interface IDependency<T>
{
    public void DoSomething();
}

class Dependency<T> : IDependency<T>
{
    public void DoSomething()
    {
    }
}

class Facade
{
    [Bind(typeof(IDependency<TT>))]
    public IDependency<T> GetDependency<T>() => new Dependency<T>();
}

interface IService
{
    public void DoSomething();
}

class Service(IDependency<int> dep) : IService
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
      if (_root._singletonFacade43 is null)
      {
        using (_lock.EnterScope())
        {
          if (_root._singletonFacade43 is null)
          {
            _root._singletonFacade43 = new Facade();
          }
        }
      }

      IDependency<int> transientIDependency1;
      Facade localInstance_1182D12746 = _root._singletonFacade43!;
      transientIDependency1 = localInstance_1182D12746.GetDependency<int>();
      return new Service(transientIDependency1);
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
	Service *--  IDependencyᐸInt32ᐳ : IDependencyᐸInt32ᐳ
	IDependencyᐸInt32ᐳ o-- "Singleton" Facade : Facade
	namespace Pure.DI.UsageTests.Basics.BindAttributeForGenericTypeScenario {
		class Composition {
		<<partial>>
		+IService Root
		}
		class Facade {
			+Facade()
		}
		class IDependencyᐸInt32ᐳ {
				<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
			+Service(IDependencyᐸInt32ᐳ dep)
		}
	}
```

