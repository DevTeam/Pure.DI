#### Tracking async disposable instances in delegates


```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var queryService1 = composition.QueryService;
var queryService2 = composition.QueryService;

await queryService2.DisposeAsync();

// Checks that the disposable instances
// associated with queryService2 have been disposed of
queryService2.Connection.IsDisposed.ShouldBeTrue();

// Checks that the disposable instances
// associated with queryService1 have not been disposed of
queryService1.Connection.IsDisposed.ShouldBeFalse();

await queryService1.DisposeAsync();

// Checks that the disposable instances
// associated with queryService1 have been disposed of
queryService1.Connection.IsDisposed.ShouldBeTrue();

interface IConnection
{
    bool IsDisposed { get; }
}

class DbConnection : IConnection, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IQueryService
{
    public IConnection Connection { get; }
}

class QueryService(Func<Owned<IConnection>> connectionFactory)
    : IQueryService, IAsyncDisposable
{
    // The Owned<T> generic type allows you to manage the lifetime of a dependency
    // explicitly. In this case, the QueryService creates the connection
    // using a factory and takes ownership of it.
    private readonly Owned<IConnection> _connection = connectionFactory();

    public IConnection Connection => _connection.Value;

    public ValueTask DisposeAsync()
    {
        // When the service is disposed, it also disposes of the connection it owns
        return _connection.DisposeAsync();
    }
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind<IConnection>().To<DbConnection>()
            .Bind().To<QueryService>()

            // Composition root
            .Root<QueryService>("QueryService");
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
    _lock = parentScope._lock;
  }

  public QueryService QueryService
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var perBlockOwned2 = new Owned();
      Func<Owned<IConnection>> transientFunc1 = new Func<Owned<IConnection>>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
        Owned<IConnection> transientOwned3;
        // Creates the owner of an instance
        Owned transientOwned4;
        Owned localOwned1 = perBlockOwned2;
        transientOwned4 = localOwned1;
        lock (_lock)
        {
          perBlockOwned2.Add(transientOwned4);
        }

        IOwned localOwned = transientOwned4;
        var transientDbConnection5 = new DbConnection();
        lock (_lock)
        {
          perBlockOwned2.Add(transientDbConnection5);
        }

        IConnection localValue2 = transientDbConnection5;
        transientOwned3 = new Owned<IConnection>(localValue2, localOwned);
        lock (_lock)
        {
          perBlockOwned2.Add(transientOwned3);
        }

        Owned<IConnection> localValue1 = transientOwned3;
        return localValue1;
      });
      return new QueryService(transientFunc1);
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
	DbConnection --|> IConnection
	QueryService --|> IQueryService
	QueryService --|> IAsyncDisposable
	Composition ..> QueryService : QueryService QueryService
	QueryService o-- "PerBlock" FuncᐸOwnedᐸIConnectionᐳᐳ : FuncᐸOwnedᐸIConnectionᐳᐳ
	FuncᐸOwnedᐸIConnectionᐳᐳ o-- "PerBlock" OwnedᐸIConnectionᐳ : OwnedᐸIConnectionᐳ
	OwnedᐸIConnectionᐳ *--  Owned : IOwned
	OwnedᐸIConnectionᐳ *--  DbConnection : IConnection
	namespace Pure.DI {
		class IOwned {
			<<interface>>
		}
		class Owned {
				<<class>>
		}
		class OwnedᐸIConnectionᐳ {
				<<struct>>
		}
	}
	namespace Pure.DI.UsageTests.Advanced.TrackingAsyncDisposableInDelegatesScenario {
		class Composition {
		<<partial>>
		+QueryService QueryService
		}
		class DbConnection {
				<<class>>
			+DbConnection()
		}
		class IConnection {
			<<interface>>
		}
		class IQueryService {
			<<interface>>
		}
		class QueryService {
				<<class>>
			+QueryService(FuncᐸOwnedᐸIConnectionᐳᐳ connectionFactory)
		}
	}
	namespace System {
		class FuncᐸOwnedᐸIConnectionᐳᐳ {
				<<delegate>>
		}
		class IAsyncDisposable {
			<<interface>>
		}
	}
```

