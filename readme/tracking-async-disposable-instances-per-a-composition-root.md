#### Tracking async disposable instances per a composition root


```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var root1 = composition.Root;
var root2 = composition.Root;

await root2.DisposeAsync();

// Checks that the disposable instances
// associated with root1 have been disposed of
root2.Value.Dependency.IsDisposed.ShouldBeTrue();

// Checks that the disposable instances
// associated with root2 have not been disposed of
root1.Value.Dependency.IsDisposed.ShouldBeFalse();

await root1.DisposeAsync();

// Checks that the disposable instances
// associated with root2 have been disposed of
root1.Value.Dependency.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<Dependency>()
            .Bind().To<Service>()

            // A special composition root
            // that allows to manage disposable dependencies
            .Root<Owned<IService>>("Root");
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
#if NET9_0_OR_GREATER
  private readonly Lock _lock;
#else
  private readonly Object _lock;
#endif

  [OrdinalAttribute(256)]
  public Composition()
  {
#if NET9_0_OR_GREATER
    _lock = new Lock();
#else
    _lock = new Object();
#endif
  }

  internal Composition(Composition parentScope)
  {
  }

  public Owned<IService> Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var blockOwned1 = new Owned();
      Owned<IService> blockOwned;
      // Creates the owner of an instance
      Owned transOwned2;
      Owned localOwned3 = blockOwned1;
      transOwned2 = localOwned3;
      lock (_lock)
      {
        blockOwned1.Add(transOwned2);
      }

      IOwned localOwned2 = transOwned2;
      var transDependency4 = new Dependency();
      lock (_lock)
      {
        blockOwned1.Add(transDependency4);
      }

      IService localValue3 = new Service(transDependency4);
      blockOwned = new Owned<IService>(localValue3, localOwned2);
      lock (_lock)
      {
        blockOwned1.Add(blockOwned);
      }

      return blockOwned;
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
	Dependency --|> IAsyncDisposable
	Service --|> IService
	Composition ..> OwnedᐸIServiceᐳ : OwnedᐸIServiceᐳ Root
	Service *--  Dependency : IDependency
	OwnedᐸIServiceᐳ *--  Owned : IOwned
	OwnedᐸIServiceᐳ *--  Service : IService
	namespace Pure.DI {
		class IOwned {
			<<interface>>
		}
		class Owned {
				<<class>>
		}
		class OwnedᐸIServiceᐳ {
				<<struct>>
		}
	}
	namespace Pure.DI.UsageTests.Advanced.TrackingAsyncDisposableScenario {
		class Composition {
		<<partial>>
		+OwnedᐸIServiceᐳ Root
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
			+Service(IDependency dependency)
		}
	}
	namespace System {
		class IAsyncDisposable {
			<<interface>>
		}
	}
```

