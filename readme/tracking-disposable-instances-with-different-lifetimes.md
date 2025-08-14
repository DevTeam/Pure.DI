#### Tracking disposable instances with different lifetimes


```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var root1 = composition.Root;
var root2 = composition.Root;
root1.Dependency.ShouldNotBe(root2.Dependency);
root1.SingleDependency.ShouldBe(root2.SingleDependency);

root2.Dispose();

// Checks that the disposable instances
// associated with root1 have been disposed of
root2.Dependency.IsDisposed.ShouldBeTrue();

// But the singleton is still not disposed of
root2.SingleDependency.IsDisposed.ShouldBeFalse();

// Checks that the disposable instances
// associated with root2 have not been disposed of
root1.Dependency.IsDisposed.ShouldBeFalse();
root1.SingleDependency.IsDisposed.ShouldBeFalse();

root1.Dispose();

// Checks that the disposable instances
// associated with root2 have been disposed of
root1.Dependency.IsDisposed.ShouldBeTrue();

// But the singleton is still not disposed of
root1.SingleDependency.IsDisposed.ShouldBeFalse();
        
composition.Dispose();
root1.SingleDependency.IsDisposed.ShouldBeTrue();

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

    public IDependency SingleDependency { get; }
}

class Service(
    Func<Owned<IDependency>> dependencyFactory,
    [Tag("single")] Func<Owned<IDependency>> singleDependencyFactory)
    : IService, IDisposable
{
    private readonly Owned<IDependency> _dependency = dependencyFactory();
    private readonly Owned<IDependency> _singleDependency = singleDependencyFactory();

    public IDependency Dependency => _dependency.Value;

    public IDependency SingleDependency => _singleDependency.Value;

    public void Dispose()
    {
        _dependency.Dispose();
        _singleDependency.Dispose();
    }
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<Dependency>()
            .Bind("single").As(Lifetime.Singleton).To<Dependency>()
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
partial class Composition: IDisposable
{
  private readonly Composition _root;
#if NET9_0_OR_GREATER
  private readonly Lock _lock;
#else
  private readonly Object _lock;
#endif
  private object[] _disposables;
  private int _disposeIndex;

  private Dependency? _singleDependency53;

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
#if NET9_0_OR_GREATER
    _lock = new Lock();
#else
    _lock = new Object();
#endif
    _disposables = new object[1];
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = parentScope._lock;
    _disposables = parentScope._disposables;
  }

  public Service Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var blockOwned3 = new Owned();
      Func<Owned<IDependency>> blockFunc1 = new Func<Owned<IDependency>>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
        Owned<IDependency> blockOwned4; // Creates the owner of an instance
        Owned transOwned5;
        Owned localOwned9 = blockOwned3;
        transOwned5 = localOwned9;
        lock (_lock)
        {
          blockOwned3.Add(transOwned5);
        }

        IOwned localOwned8 = transOwned5;
        var transDependency6 = new Dependency();
        lock (_lock)
        {
          blockOwned3.Add(transDependency6);
        }

        IDependency localValue12 = transDependency6;
        blockOwned4 = new Owned<IDependency>(localValue12, localOwned8);
        lock (_lock)
        {
          blockOwned3.Add(blockOwned4);
        }

        Owned<IDependency> localValue11 = blockOwned4;
        return localValue11;
      });
      var blockOwned7 = new Owned();
      Func<Owned<IDependency>> blockFunc2 = new Func<Owned<IDependency>>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
        Owned<IDependency> blockOwned8; // Creates the owner of an instance
        Owned transOwned9;
        Owned localOwned11 = blockOwned7;
        transOwned9 = localOwned11;
        lock (_lock)
        {
          blockOwned7.Add(transOwned9);
        }

        IOwned localOwned10 = transOwned9;
        if (_root._singleDependency53 is null)
        {
          lock (_lock)
          {
            _root._singleDependency53 = new Dependency();
            _root._disposables[_root._disposeIndex++] = _root._singleDependency53;
          }
        }

        IDependency localValue14 = _root._singleDependency53;
        blockOwned8 = new Owned<IDependency>(localValue14, localOwned10);
        lock (_lock)
        {
          blockOwned7.Add(blockOwned8);
        }

        Owned<IDependency> localValue13 = blockOwned8;
        return localValue13;
      });
      return new Service(blockFunc1, blockFunc2);
    }
  }

  public void Dispose()
  {
    int disposeIndex;
    object[] disposables;
    lock (_lock)
    {
      disposeIndex = _disposeIndex;
      _disposeIndex = 0;
      disposables = _disposables;
      _disposables = new object[1];
      _singleDependency53 = default(Dependency);
      }

      while (disposeIndex-- > 0)
      {
        switch (disposables[disposeIndex])
        {
          case IDisposable disposableInstance:
            try
            {
              disposableInstance.Dispose();
            }
            catch (Exception exception)
            {
              OnDisposeException(disposableInstance, exception);
            }
            break;
        }
      }
    }

    partial void OnDisposeException<T>(T disposableInstance, Exception exception) where T : IDisposable;
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
	Composition --|> IDisposable
	Owned --|> IOwned
	Dependency --|> IDependency
	Service --|> IService
	Composition ..> Service : Service Root
	Service o-- "PerBlock" FuncᐸOwnedᐸIDependencyᐳᐳ : FuncᐸOwnedᐸIDependencyᐳᐳ
	Service o-- "PerBlock" FuncᐸOwnedᐸIDependencyᐳᐳ : "single"  FuncᐸOwnedᐸIDependencyᐳᐳ
	FuncᐸOwnedᐸIDependencyᐳᐳ o-- "PerBlock" OwnedᐸIDependencyᐳ : OwnedᐸIDependencyᐳ
	FuncᐸOwnedᐸIDependencyᐳᐳ o-- "PerBlock" OwnedᐸIDependencyᐳ : "single"  OwnedᐸIDependencyᐳ
	OwnedᐸIDependencyᐳ *--  Owned : IOwned
	OwnedᐸIDependencyᐳ *--  Dependency : IDependency
	OwnedᐸIDependencyᐳ *--  Owned : IOwned
	OwnedᐸIDependencyᐳ o-- "Singleton" Dependency : "single"  IDependency
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
	namespace Pure.DI.UsageTests.Advanced.TrackingDisposableWithDifferentLifetimesScenario {
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
			+Service(FuncᐸOwnedᐸIDependencyᐳᐳ dependencyFactory, FuncᐸOwnedᐸIDependencyᐳᐳ singleDependencyFactory)
		}
	}
	namespace System {
		class FuncᐸOwnedᐸIDependencyᐳᐳ {
				<<delegate>>
		}
		class IDisposable {
			<<abstract>>
		}
	}
```

