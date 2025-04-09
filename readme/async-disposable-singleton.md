#### Async disposable singleton

If at least one of these objects implements the `IAsyncDisposable` interface, then the composition implements `IAsyncDisposable` as well. To dispose of all created singleton instances in an asynchronous manner, simply dispose of the composition instance in an asynchronous manner:


```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind().As(Singleton).To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

IDependency dependency;
await using (var composition = new Composition())
{
    var service = composition.Root;
    dependency = service.Dependency;
}

dependency.IsDisposed.ShouldBeTrue();

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
partial class Composition: IDisposable, IAsyncDisposable
{
  private readonly Composition _root;
  private readonly Lock _lock;
  private object[] _disposables;
  private int _disposeIndex;

  private Dependency? _singletonDependency51;

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
    _lock = new Lock();
    _disposables = new object[1];
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = _root._lock;
    _disposables = parentScope._disposables;
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_root._singletonDependency51 is null)
      {
        using (_lock.EnterScope())
        {
          if (_root._singletonDependency51 is null)
          {
            _root._singletonDependency51 = new Dependency();
            _root._disposables[_root._disposeIndex++] = _root._singletonDependency51;
          }
        }
      }

      return new Service(_root._singletonDependency51);
    }
  }

  public void Dispose()
  {
    int disposeIndex;
    object[] disposables;
    using (_lock.EnterScope())
    {
      disposeIndex = _disposeIndex;
      _disposeIndex = 0;
      disposables = _disposables;
      _disposables = new object[1];
      _singletonDependency51 = null;
      }

      while (disposeIndex-- > 0)
      {
        switch (disposables[disposeIndex])
        {
          case IAsyncDisposable asyncDisposableInstance:
            try
            {
              var valueTask = asyncDisposableInstance.DisposeAsync();
              if (!valueTask.IsCompleted)
              {
                valueTask.AsTask().Wait();
              }
            }
            catch (Exception exception)
            {
              OnDisposeAsyncException(asyncDisposableInstance, exception);
            }
            break;
        }
      }
    }

    partial void OnDisposeException<T>(T disposableInstance, Exception exception) where T : IDisposable;

    public async ValueTask DisposeAsync()
    {
      int disposeIndex;
      object[] disposables;
      _lock.Enter();
      try
      {
        disposeIndex = _disposeIndex;
        _disposeIndex = 0;
        disposables = _disposables;
        _disposables = new object[1];
        _singletonDependency51 = null;
        }
        finally
        {
          _lock.Exit();
        }

        while (disposeIndex-- > 0)
        {
          switch (disposables[disposeIndex])
          {
            case IAsyncDisposable asyncDisposableInstance:
              try
              {
                await asyncDisposableInstance.DisposeAsync();
              }
              catch (Exception exception)
              {
                OnDisposeAsyncException(asyncDisposableInstance, exception);
              }
              break;
          }
        }
      }

      partial void OnDisposeAsyncException<T>(T asyncDisposableInstance, Exception exception) where T : IAsyncDisposable;
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
	Composition --|> IDisposable
	Composition --|> IAsyncDisposable
	Service --|> IService
	Dependency --|> IDependency
	Dependency --|> IAsyncDisposable
	Composition ..> Service : IService Root
	Service o-- "Singleton" Dependency : IDependency
	namespace Pure.DI.UsageTests.Lifetimes.AsyncDisposableSingletonScenario {
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
			+Service(IDependency dependency)
		}
	}
	namespace System {
		class IAsyncDisposable {
			<<abstract>>
		}
		class IDisposable {
			<<abstract>>
		}
	}
```

