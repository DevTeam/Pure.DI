#### PerResolve

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Lifetimes/PerResolveScenario.cs)

The _PerResolve_ lifetime ensures that there will be one instance of the dependency for each composition root instance.


```c#
interface IDependency;

class Dependency : IDependency;

class Service(
    IDependency dep1,
    IDependency dep2,
    (IDependency dep3, IDependency dep4) deps)
{
    public IDependency Dep1 { get; } = dep1;

    public IDependency Dep2 { get; } = dep2;

    public IDependency Dep3 { get; } = deps.dep3;

    public IDependency Dep4 { get; } = deps.dep4;
}

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind().As(Lifetime.PerResolve).To<Dependency>()
    .Bind().As(Lifetime.Singleton).To<(IDependency dep3, IDependency dep4)>()

    // Composition root
    .Root<Service>("Root");

var composition = new Composition();

var service1 = composition.Root;
service1.Dep1.ShouldBe(service1.Dep2);
service1.Dep3.ShouldBe(service1.Dep4);
service1.Dep1.ShouldBe(service1.Dep3);

var service2 = composition.Root;
service2.Dep1.ShouldNotBe(service1.Dep1);
```

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly Lock _lock;

  private (IDependency dep3, IDependency dep4) _singletonValueTuple44;
  private bool _singletonValueTuple44Created;

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

  public Service Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var perResolveDependency43 = default(Dependency);
      if (!_root._singletonValueTuple44Created)
      {
        using (_lock.EnterScope())
        {
          if (!_root._singletonValueTuple44Created)
          {
            if (perResolveDependency43 is null)
            {
              perResolveDependency43 = new Dependency();
            }

            _root._singletonValueTuple44 = (perResolveDependency43, perResolveDependency43);
            Thread.MemoryBarrier();
            _root._singletonValueTuple44Created = true;
          }
        }
      }

      if (perResolveDependency43 is null)
      {
        using (_lock.EnterScope())
        {
          if (perResolveDependency43 is null)
          {
            perResolveDependency43 = new Dependency();
          }
        }
      }

      return new Service(perResolveDependency43, perResolveDependency43, _root._singletonValueTuple44);
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
	Dependency --|> IDependency
	Composition ..> Service : Service Root
	Service o-- "2 PerResolve instances" Dependency : IDependency
	Service o-- "Singleton" ValueTupleᐸIDependencyˏIDependencyᐳ : ValueTupleᐸIDependencyˏIDependencyᐳ
	ValueTupleᐸIDependencyˏIDependencyᐳ o-- "2 PerResolve instances" Dependency : IDependency
	namespace Pure.DI.UsageTests.Lifetimes.PerResolveScenario {
		class Composition {
		<<partial>>
		+Service Root
		}
		class Dependency {
			+Dependency()
		}
		class IDependency {
			<<interface>>
		}
		class Service {
		}
	}
	namespace System {
		class ValueTupleᐸIDependencyˏIDependencyᐳ {
				<<struct>>
			+ValueTuple(IDependency item1, IDependency item2)
		}
	}
```

