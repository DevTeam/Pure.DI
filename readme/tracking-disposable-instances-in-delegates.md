#### Tracking disposable instances in delegates


```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var root1 = composition.Root;
var root2 = composition.Root;

root2.Dispose();

// Checks that the disposable instances
// associated with root1 have been disposed of
root2.Dependency.IsDisposed.ShouldBeTrue();

// Checks that the disposable instances
// associated with root2 have not been disposed of
root1.Dependency.IsDisposed.ShouldBeFalse();

root1.Dispose();

// Checks that the disposable instances
// associated with root2 have been disposed of
root1.Dependency.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(Func<Owned<IDependency>> dependencyFactory)
    : IService, IDisposable
{
    private readonly Owned<IDependency> _dependency = dependencyFactory();

    public IDependency Dependency => _dependency.Value;

    public void Dispose() => _dependency.Dispose();
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<Dependency>()
            .Bind().To<Service>()

            // Composition root
            .Root<Service>("Root");
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

  public Service Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var accumulator56 = new Owned();
      Func<Owned<IDependency>> perBlockFunc1 = new Func<Owned<IDependency>>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
        var accumulator56 = new Owned();
        Owned transientOwned3;
        Owned localOwned61 = accumulator56;
        transientOwned3 = localOwned61;
        lock (_lock)
        {
          accumulator56.Add(transientOwned3);
        }

        Dependency transientDependency4 = new Dependency();
        lock (_lock)
        {
          accumulator56.Add(transientDependency4);
        }

        Owned<IDependency> perBlockOwned2; // Creates the owner of an instance
        IOwned localOwned62 = transientOwned3;
        IDependency localValue63 = transientDependency4;
        perBlockOwned2 = new Owned<IDependency>(localValue63, localOwned62);
        lock (_lock)
        {
          accumulator56.Add(perBlockOwned2);
        }

        Owned<IDependency> localValue60 = perBlockOwned2;
        return localValue60;
      });
      Service transientService0 = new Service(perBlockFunc1);
      lock (_lock)
      {
        accumulator56.Add(transientService0);
      }

      return transientService0;
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
	Owned --|> IOwned
	Dependency --|> IDependency
	Service --|> IService
	Composition ..> Service : Service Root
	Service o-- "PerBlock" FuncᐸOwnedᐸIDependencyᐳᐳ : FuncᐸOwnedᐸIDependencyᐳᐳ
	FuncᐸOwnedᐸIDependencyᐳᐳ o-- "PerBlock" OwnedᐸIDependencyᐳ : OwnedᐸIDependencyᐳ
	OwnedᐸIDependencyᐳ *--  Owned : IOwned
	OwnedᐸIDependencyᐳ *--  Dependency : IDependency
	namespace Pure.DI {
		class IOwned {
			<<interface>>
		}
		class Owned {
				<<class>>
		}
		class OwnedᐸIDependencyᐳ {
				<<struct>>
		}
	}
	namespace Pure.DI.UsageTests.Advanced.TrackingDisposableInDelegatesScenario {
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
		class IService {
			<<interface>>
		}
		class Service {
				<<class>>
			+Service(FuncᐸOwnedᐸIDependencyᐳᐳ dependencyFactory)
		}
	}
	namespace System {
		class FuncᐸOwnedᐸIDependencyᐳᐳ {
				<<delegate>>
		}
	}
```

