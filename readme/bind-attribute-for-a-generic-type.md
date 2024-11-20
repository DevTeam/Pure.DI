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
  private readonly object _lock;

  private Facade? _singletonFacade39;

  [OrdinalAttribute(20)]
  public Composition()
  {
    _root = this;
    _lock = new object();
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
      if (_root._singletonFacade39 is null)
      {
        lock (_lock)
        {
          if (_root._singletonFacade39 is null)
          {
            _root._singletonFacade39 = new Facade();
          }
        }
      }

      IDependency<int> transientIDependency1;
      Facade localInstance_1182D12730 = _root._singletonFacade39!;
      transientIDependency1 = localInstance_1182D12730.GetDependency<int>();
      return new Service(transientIDependency1);
    }
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+IService Root
	}
	Service --|> IService
	class Service {
		+Service(IDependencyᐸInt32ᐳ dep)
	}
	class IDependencyᐸInt32ᐳ
	class Facade {
		+Facade()
	}
	class IService {
		<<interface>>
	}
	Composition ..> Service : IService Root
	Service *--  IDependencyᐸInt32ᐳ : IDependencyᐸInt32ᐳ
	IDependencyᐸInt32ᐳ o-- "Singleton" Facade : Facade
```

