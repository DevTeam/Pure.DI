#### Tracking disposable instances using pre-built classes

If you want ready-made classes for tracking disposable objects in your libraries but don't want to create your own, you can add this package to your projects:

[![NuGet](https://img.shields.io/nuget/v/Pure.DI.Abstractions)](https://www.nuget.org/packages/Pure.DI.Abstractions)

It contains attributes like `Inject` and `Inject<T>` that work for constructors and their arguments, methods and their arguments, properties and fields. They allow you to setup all injection parameters.


```c#
using Shouldly;
using Pure.DI.Abstractions;
using Pure.DI;

var composition = new Composition();
var dataService1 = composition.DataService;
var dataService2 = composition.DataService;

// The dedicated connection should be unique for each root
dataService1.Connection.ShouldNotBe(dataService2.Connection);

// The shared connection should be the same instance
dataService1.SharedConnection.ShouldBe(dataService2.SharedConnection);

dataService2.Dispose();

// Checks that the disposable instances
// associated with dataService2 have been disposed of
dataService2.Connection.IsDisposed.ShouldBeTrue();

// But the singleton is still not disposed of
// because it is shared and tracked by the composition
dataService2.SharedConnection.IsDisposed.ShouldBeFalse();

// Checks that the disposable instances
// associated with dataService1 have not been disposed of
dataService1.Connection.IsDisposed.ShouldBeFalse();
dataService1.SharedConnection.IsDisposed.ShouldBeFalse();

dataService1.Dispose();

// Checks that the disposable instances
// associated with dataService1 have been disposed of
dataService1.Connection.IsDisposed.ShouldBeTrue();

// But the singleton is still not disposed of
dataService1.SharedConnection.IsDisposed.ShouldBeFalse();

composition.Dispose();

// The shared singleton is disposed only when the composition is disposed
dataService1.SharedConnection.IsDisposed.ShouldBeTrue();

interface IDbConnection
{
    bool IsDisposed { get; }
}

class DbConnection : IDbConnection, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IDataService
{
    public IDbConnection Connection { get; }

    public IDbConnection SharedConnection { get; }
}

class DataService(
    Func<Own<IDbConnection>> connectionFactory,
    [Tag("shared")] Func<Own<IDbConnection>> sharedConnectionFactory)
    : IDataService, IDisposable
{
    // Own<T> is a wrapper from Pure.DI.Abstractions that owns the value.
    // It ensures that the value is disposed when Own<T> is disposed,
    // but only if the value is not a singleton or externally owned.
    private readonly Own<IDbConnection> _connection = connectionFactory();
    private readonly Own<IDbConnection> _sharedConnection = sharedConnectionFactory();

    public IDbConnection Connection => _connection.Value;

    public IDbConnection SharedConnection => _sharedConnection.Value;

    public void Dispose()
    {
        // Disposes the dedicated connection
        _connection.Dispose();

        // Notifies that we are done with the shared connection.
        // However, since it's a singleton, the underlying instance won't be disposed here.
        _sharedConnection.Dispose();
    }
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<DbConnection>()
            .Bind("shared").As(Lifetime.Singleton).To<DbConnection>()
            .Bind().To<DataService>()

            // Composition root
            .Root<DataService>("DataService");
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
  - [Pure.DI.Abstractions](https://www.nuget.org/packages/Pure.DI.Abstractions)
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
dotnet add package Pure.DI.Abstractions
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

This package should also be included in a project:

[![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)

The following partial class will be generated:

```c#
partial class Composition: IDisposable
{
#if NET9_0_OR_GREATER
  private readonly Lock _lock = new Lock();
#else
  private readonly Object _lock = new Object();
#endif
  private object[] _disposables = new object[1];
  private int _disposeIndex;

  private DbConnection? _singletonDbConnection52;

  public DataService DataService
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var perBlockOwn3 = new Abstractions.Own();
      Func<Abstractions.Own<IDbConnection>> transientFunc1 = new Func<Abstractions.Own<IDbConnection>>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
        Abstractions.Own<IDbConnection> transientOwn4;
        // Creates the owner of an instance
        Abstractions.Own localOwn = perBlockOwn3;
        var transientDbConnection5 = new DbConnection();
        lock (_lock)
        {
          perBlockOwn3.Add(transientDbConnection5);
        }

        IDbConnection localValue8 = transientDbConnection5;
        transientOwn4 = new Abstractions.Own<IDbConnection>(localValue8, localOwn);
        lock (_lock)
        {
          perBlockOwn3.Add(transientOwn4);
        }

        Abstractions.Own<IDbConnection> localValue7 = transientOwn4;
        return localValue7;
      });
      var perBlockOwn6 = new Abstractions.Own();
      Func<Abstractions.Own<IDbConnection>> transientFunc2 = new Func<Abstractions.Own<IDbConnection>>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
        Abstractions.Own<IDbConnection> transientOwn7;
        // Creates the owner of an instance
        Abstractions.Own localOwn1 = perBlockOwn6;
        if (_singletonDbConnection52 is null)
          lock (_lock)
            if (_singletonDbConnection52 is null)
            {
              _singletonDbConnection52 = new DbConnection();
              _disposables[_disposeIndex++] = _singletonDbConnection52;
            }

        IDbConnection localValue10 = _singletonDbConnection52;
        transientOwn7 = new Abstractions.Own<IDbConnection>(localValue10, localOwn1);
        lock (_lock)
        {
          perBlockOwn6.Add(transientOwn7);
        }

        Abstractions.Own<IDbConnection> localValue9 = transientOwn7;
        return localValue9;
      });
      return new DataService(transientFunc1, transientFunc2);
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
      _singletonDbConnection52 = null;
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
	DbConnection --|> IDbConnection
	DataService --|> IDataService
	Composition ..> DataService : DataService DataService
	DataService o-- "PerBlock" FuncᐸOwnᐸIDbConnectionᐳᐳ : FuncᐸOwnᐸIDbConnectionᐳᐳ
	DataService o-- "PerBlock" FuncᐸOwnᐸIDbConnectionᐳᐳ : "shared"  FuncᐸOwnᐸIDbConnectionᐳᐳ
	FuncᐸOwnᐸIDbConnectionᐳᐳ o-- "PerBlock" OwnᐸIDbConnectionᐳ : OwnᐸIDbConnectionᐳ
	FuncᐸOwnᐸIDbConnectionᐳᐳ o-- "PerBlock" OwnᐸIDbConnectionᐳ : "shared"  OwnᐸIDbConnectionᐳ
	OwnᐸIDbConnectionᐳ *--  DbConnection : IDbConnection
	OwnᐸIDbConnectionᐳ o-- "PerBlock" Own : Own
	OwnᐸIDbConnectionᐳ o-- "Singleton" DbConnection : "shared"  IDbConnection
	OwnᐸIDbConnectionᐳ o-- "PerBlock" Own : Own
	namespace Pure.DI.Abstractions {
		class Own {
				<<class>>
		}
		class OwnᐸIDbConnectionᐳ {
				<<struct>>
		}
	}
	namespace Pure.DI.UsageTests.Advanced.TrackingDisposableWithAbstractionsScenario {
		class Composition {
		<<partial>>
		+DataService DataService
		}
		class DataService {
				<<class>>
			+DataService(FuncᐸOwnᐸIDbConnectionᐳᐳ connectionFactory, FuncᐸOwnᐸIDbConnectionᐳᐳ sharedConnectionFactory)
		}
		class DbConnection {
				<<class>>
			+DbConnection()
		}
		class IDataService {
			<<interface>>
		}
		class IDbConnection {
			<<interface>>
		}
	}
	namespace System {
		class FuncᐸOwnᐸIDbConnectionᐳᐳ {
				<<delegate>>
		}
		class IDisposable {
			<<abstract>>
		}
	}
```

