#### PerResolve

The _PerResolve_ lifetime ensures that there will be one instance of the dependency for each composition root instance.


```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    .Bind().As(PerResolve).To<Dependency>()
    .Bind().As(Singleton).To<(IDependency dep3, IDependency dep4)>()

    // Composition root
    .Root<Service>("Root");

var composition = new Composition();

var service1 = composition.Root;
service1.Dep1.ShouldBe(service1.Dep2);
service1.Dep3.ShouldBe(service1.Dep4);
service1.Dep1.ShouldBe(service1.Dep3);

var service2 = composition.Root;
service2.Dep1.ShouldNotBe(service1.Dep1);

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
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later is installed
```bash
dotnet --list-sdk
```
- Create a net10.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to NuGet packages
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
  - [Shouldly](https://www.nuget.org/packages/Shouldly)
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
#if NET9_0_OR_GREATER
  private readonly Lock _lock;
#else
  private readonly Object _lock;
#endif

  private (IDependency dep3, IDependency dep4) _singletonValueTuple52;
  private bool _singletonValueTuple52Created;

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
#if NET9_0_OR_GREATER
    _lock = new Lock();
#else
    _lock = new Object();
#endif
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = parentScope._lock;
  }

  public Service Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var perResolveDependency1 = default(Dependency);
      if (!_root._singletonValueTuple52Created)
        lock (_lock)
          if (!_root._singletonValueTuple52Created)
          {
            EnsureDependencyExists();
            _root._singletonValueTuple52 = (perResolveDependency1, perResolveDependency1);
            Thread.MemoryBarrier();
            _root._singletonValueTuple52Created = true;
          }

      return new Service(perResolveDependency1, perResolveDependency1, _root._singletonValueTuple52);
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      void EnsureDependencyExists()
      {
        if (perResolveDependency1 is null)
          lock (_lock)
            if (perResolveDependency1 is null)
            {
              perResolveDependency1 = new Dependency();
            }
      }
    }
  }
}
```

Class diagram:

```mermaid
---
 config:
  maxTextSize: 2147483647
  maxEdges: 2147483647
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Dependency --|> IDependency
	Composition ..> Service : Service Root
	ValueTupleᐸIDependencyˏIDependencyᐳ o-- "2 PerResolve instances" Dependency : IDependency
	Service o-- "2 PerResolve instances" Dependency : IDependency
	Service o-- "Singleton" ValueTupleᐸIDependencyˏIDependencyᐳ : ValueTupleᐸIDependencyˏIDependencyᐳ
	namespace Pure.DI.UsageTests.Lifetimes.PerResolveScenario {
		class Composition {
		<<partial>>
		+Service Root
		}
		class Dependency {
				<<class>>
			+Dependency()
		}
		class IDependency {
			<<interface>>
		}
		class Service {
				<<class>>
			+Service(IDependency dep1, IDependency dep2, ValueTupleᐸIDependencyˏIDependencyᐳ deps)
		}
	}
	namespace System {
		class ValueTupleᐸIDependencyˏIDependencyᐳ {
				<<struct>>
			+ValueTuple(IDependency item1, IDependency item2)
		}
	}
```

