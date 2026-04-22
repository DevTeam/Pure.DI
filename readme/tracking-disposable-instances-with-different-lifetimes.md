#### Tracking disposable instances with different lifetimes

Demonstrates how disposable instances with different lifetimes are tracked and disposed correctly according to their respective lifetime scopes.


```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var queryHandler1 = composition.QueryHandler;
var queryHandler2 = composition.QueryHandler;

// The exclusive connection is created for each handler
queryHandler1.ExclusiveConnection.ShouldNotBe(queryHandler2.ExclusiveConnection);

// The shared connection is the same for all handlers
queryHandler1.SharedConnection.ShouldBe(queryHandler2.SharedConnection);

// Disposing the second handler
queryHandler2.Dispose();

// Checks that the exclusive connection
// associated with queryHandler2 has been disposed of
queryHandler2.ExclusiveConnection.IsDisposed.ShouldBeTrue();

// But the shared connection is still alive (not disposed)
// because it is a Singleton and shared with other consumers
queryHandler2.SharedConnection.IsDisposed.ShouldBeFalse();

// Checks that the connections associated with root1
// are not affected by queryHandler2 disposal
queryHandler1.ExclusiveConnection.IsDisposed.ShouldBeFalse();
queryHandler1.SharedConnection.IsDisposed.ShouldBeFalse();

// Disposing the first handler
queryHandler1.Dispose();

// Its exclusive connection is now disposed
queryHandler1.ExclusiveConnection.IsDisposed.ShouldBeTrue();

// The shared connection is STILL alive
queryHandler1.SharedConnection.IsDisposed.ShouldBeFalse();

// Disposing the  root composition
// This should dispose all Singletons
composition.Dispose();

// Now the shared connection is disposed
queryHandler1.SharedConnection.IsDisposed.ShouldBeTrue();

interface IConnection
{
    bool IsDisposed { get; }
}

class Connection : IConnection, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IQueryHandler
{
    public IConnection ExclusiveConnection { get; }

    public IConnection SharedConnection { get; }
}

class QueryHandler(
    Func<Owned<IConnection>> connectionFactory,
    [Tag("Shared")] Func<Owned<IConnection>> sharedConnectionFactory)
    : IQueryHandler, IDisposable
{
    private readonly Owned<IConnection> _exclusiveConnection = connectionFactory();
    private readonly Owned<IConnection> _sharedConnection = sharedConnectionFactory();

    public IConnection ExclusiveConnection => _exclusiveConnection.Value;

    public IConnection SharedConnection => _sharedConnection.Value;

    public void Dispose()
    {
        // Disposes the owned instances.
        // For the exclusive connection (Transient), this disposes the actual connection.
        // For the shared connection (Singleton), this just releases the ownership
        // but does NOT dispose the underlying singleton instance until the composition is disposed.
        _exclusiveConnection.Dispose();
        _sharedConnection.Dispose();
    }
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<Connection>()
            .Bind("Shared").As(Lifetime.Singleton).To<Connection>()
            .Bind().To<QueryHandler>()

            // Composition root
            .Root<QueryHandler>("QueryHandler");
}
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later installed
```bash
dotnet --list-sdk
```
- Create a net10.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to the NuGet packages
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

>[!NOTE]
>The tracking mechanism respects lifetime semantics, ensuring that transient instances are disposed immediately while singleton instances persist until composition disposal.

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

  private Connection? _singletonConnection63;

  public QueryHandler QueryHandler
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var perBlockOwned185 = new Owned();
      Func<Owned<IConnection>> perBlockFunc183 = new Func<Owned<IConnection>>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
        Owned<IConnection> perBlockOwned186;
        // Creates the owner of an instance
        Owned transientOwned187;
        Owned localOwned9 = perBlockOwned185;
        transientOwned187 = localOwned9;
        lock (_lock)
        {
          perBlockOwned185.Add(transientOwned187);
        }

        IOwned localOwned8 = transientOwned187;
        var transientConnection188 = new Connection();
        lock (_lock)
        {
          perBlockOwned185.Add(transientConnection188);
        }

        IConnection localValue16 = transientConnection188;
        perBlockOwned186 = new Owned<IConnection>(localValue16, localOwned8);
        lock (_lock)
        {
          perBlockOwned185.Add(perBlockOwned186);
        }

        return perBlockOwned186;
      });
      var perBlockOwned189 = new Owned();
      Func<Owned<IConnection>> perBlockFunc184 = new Func<Owned<IConnection>>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
        Owned<IConnection> perBlockOwned190;
        // Creates the owner of an instance
        Owned transientOwned191;
        Owned localOwned11 = perBlockOwned189;
        transientOwned191 = localOwned11;
        lock (_lock)
        {
          perBlockOwned189.Add(transientOwned191);
        }

        IOwned localOwned10 = transientOwned191;
        if (_singletonConnection63 is null)
          lock (_lock)
            if (_singletonConnection63 is null)
            {
              _singletonConnection63 = new Connection();
              _disposables[_disposeIndex++] = _singletonConnection63;
            }

        IConnection localValue18 = _singletonConnection63;
        perBlockOwned190 = new Owned<IConnection>(localValue18, localOwned10);
        lock (_lock)
        {
          perBlockOwned189.Add(perBlockOwned190);
        }

        return perBlockOwned190;
      });
      return new QueryHandler(perBlockFunc183, perBlockFunc184);
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
      _singletonConnection63 = null;
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
	Connection --|> IConnection
	QueryHandler --|> IQueryHandler
	Composition ..> QueryHandler : QueryHandler QueryHandler
	QueryHandler o-- "PerBlock" FuncᐸOwnedᐸIConnectionᐳᐳ : FuncᐸOwnedᐸIConnectionᐳᐳ
	QueryHandler o-- "PerBlock" FuncᐸOwnedᐸIConnectionᐳᐳ : "Shared"  FuncᐸOwnedᐸIConnectionᐳᐳ
	FuncᐸOwnedᐸIConnectionᐳᐳ o-- "PerBlock" OwnedᐸIConnectionᐳ : OwnedᐸIConnectionᐳ
	FuncᐸOwnedᐸIConnectionᐳᐳ o-- "PerBlock" OwnedᐸIConnectionᐳ : "Shared"  OwnedᐸIConnectionᐳ
	OwnedᐸIConnectionᐳ *--  Owned : IOwned
	OwnedᐸIConnectionᐳ *--  Connection : IConnection
	OwnedᐸIConnectionᐳ *--  Owned : IOwned
	OwnedᐸIConnectionᐳ o-- "Singleton" Connection : "Shared"  IConnection
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
	namespace Pure.DI.UsageTests.Advanced.TrackingDisposableWithDifferentLifetimesScenario {
		class Composition {
		<<partial>>
		+QueryHandler QueryHandler
		}
		class Connection {
				<<class>>
			+Connection()
		}
		class IConnection {
			<<interface>>
		}
		class IQueryHandler {
			<<interface>>
		}
		class QueryHandler {
				<<class>>
			+QueryHandler(FuncᐸOwnedᐸIConnectionᐳᐳ connectionFactory, FuncᐸOwnedᐸIConnectionᐳᐳ sharedConnectionFactory)
		}
	}
	namespace System {
		class FuncᐸOwnedᐸIConnectionᐳᐳ {
				<<delegate>>
		}
		class IDisposable {
			<<abstract>>
		}
	}
```

