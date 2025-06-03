#### Generic injections as required with arguments


```c#
using Shouldly;
using Pure.DI;
using System.Collections.Generic;

DI.Setup(nameof(Composition))
    .Bind().To<Dependency<TT>>()
    .Bind().To<Service<TT>>()

    // Composition root
    .Root<IService<string>>("Root");

var composition = new Composition();
var service = composition.Root;
var dependencies = service.Dependencies;
dependencies.Count.ShouldBe(2);
dependencies[0].Id.ShouldBe(33);
dependencies[1].Id.ShouldBe(99);

interface IDependency<out T>
{
    int Id { get; }
}

class Dependency<T>(int id) : IDependency<T>
{
    public int Id { get; } = id;
}

interface IService<out T>
{
    IReadOnlyList<IDependency<T>> Dependencies { get; }
}

class Service<T>(Func<int, IDependency<T>> dependencyFactoryWithArgs): IService<T>
{
    public IReadOnlyList<IDependency<T>> Dependencies { get; } =
    [
        dependencyFactoryWithArgs(33),
        dependencyFactoryWithArgs(99)
    ];
}
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later is installed
```bash
dotnet --list-sdk
```
- Create a net9.0 (or later) console application
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
    _lock = _root._lock;
  }

  public IService<string> Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var perResolveFunc55 = default(Func<int, IDependency<string>>);
      int overInt320;
      if (perResolveFunc55 is null)
      {
        lock (_lock)
        {
          if (perResolveFunc55 is null)
          {
            Func<int, IDependency<string>> localFactory147 = new Func<int, IDependency<string>>((int localArg111) =>
            {
              Lock transientLock1 = _lock;
              Lock localLockObject148 = transientLock1;
              lock (localLockObject148)
              {
                overInt320 = localArg111;
                IDependency<string> localValue149 = new Dependency<string>(overInt320);
                return localValue149;
              }
            });
            perResolveFunc55 = localFactory147;
          }
        }
      }

      return new Service<string>(perResolveFunc55);
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
	ServiceᐸStringᐳ --|> IServiceᐸStringᐳ
	DependencyᐸStringᐳ --|> IDependencyᐸStringᐳ
	Composition ..> ServiceᐸStringᐳ : IServiceᐸStringᐳ Root
	ServiceᐸStringᐳ o-- "PerResolve" FuncᐸInt32ˏIDependencyᐸStringᐳᐳ : FuncᐸInt32ˏIDependencyᐸStringᐳᐳ
	FuncᐸInt32ˏIDependencyᐸStringᐳᐳ *--  Lock : "SyncRoot"  Lock
	FuncᐸInt32ˏIDependencyᐸStringᐳᐳ *--  DependencyᐸStringᐳ : IDependencyᐸStringᐳ
	DependencyᐸStringᐳ *--  Int32 : Int32
	namespace Pure.DI.UsageTests.Generics.GenericInjectionsAsRequiredWithArgumentsScenario {
		class Composition {
		<<partial>>
		+IServiceᐸStringᐳ Root
		}
		class DependencyᐸStringᐳ {
				<<class>>
			+Dependency(Int32 id)
		}
		class IDependencyᐸStringᐳ {
			<<interface>>
		}
		class IServiceᐸStringᐳ {
			<<interface>>
		}
		class ServiceᐸStringᐳ {
				<<class>>
			+Service(FuncᐸInt32ˏIDependencyᐸStringᐳᐳ dependencyFactoryWithArgs)
		}
	}
	namespace System {
		class FuncᐸInt32ˏIDependencyᐸStringᐳᐳ {
				<<delegate>>
		}
		class Int32 {
			<<struct>>
		}
	}
	namespace System.Threading {
		class Lock {
			<<class>>
		}
	}
```

