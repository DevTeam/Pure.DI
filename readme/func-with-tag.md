#### Func with tag

Demonstrates how to use Func<T> with tags for dynamic creation of tagged instances.


```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind<IDbConnection>("postgres").To<NpgsqlConnection>()
    .Bind<IConnectionPool>().To<ConnectionPool>()

    // Composition root
    .Root<IConnectionPool>("ConnectionPool");

var composition = new Composition();
var pool = composition.ConnectionPool;

// Check that the pool has created 3 connections
pool.Connections.Length.ShouldBe(3);
pool.Connections[0].ShouldBeOfType<NpgsqlConnection>();

interface IDbConnection;

// Specific implementation for PostgreSQL
class NpgsqlConnection : IDbConnection;

interface IConnectionPool
{
    ImmutableArray<IDbConnection> Connections { get; }
}

class ConnectionPool([Tag("postgres")] Func<IDbConnection> connectionFactory) : IConnectionPool
{
    public ImmutableArray<IDbConnection> Connections { get; } =
    [
        // Use the factory to create distinct connection instances
        connectionFactory(),
        connectionFactory(),
        connectionFactory()
    ];
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
>Func with tags allows you to create instances with specific tags dynamically, useful for factory patterns with multiple implementations.

The following partial class will be generated:

```c#
partial class Composition
{
  public IConnectionPool ConnectionPool
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Func<IDbConnection> perBlockFunc389 = new Func<IDbConnection>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
        return new NpgsqlConnection();
      });
      return new ConnectionPool(perBlockFunc389);
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
	NpgsqlConnection --|> IDbConnection : "postgres" 
	ConnectionPool --|> IConnectionPool
	Composition ..> ConnectionPool : IConnectionPool ConnectionPool
	ConnectionPool o-- "PerBlock" FuncᐸIDbConnectionᐳ : "postgres"  FuncᐸIDbConnectionᐳ
	FuncᐸIDbConnectionᐳ *--  NpgsqlConnection : "postgres"  IDbConnection
	namespace Pure.DI.UsageTests.BCL.FuncWithTagScenario {
		class Composition {
		<<partial>>
		+IConnectionPool ConnectionPool
		}
		class ConnectionPool {
				<<class>>
			+ConnectionPool(FuncᐸIDbConnectionᐳ connectionFactory)
		}
		class IConnectionPool {
			<<interface>>
		}
		class IDbConnection {
			<<interface>>
		}
		class NpgsqlConnection {
				<<class>>
			+NpgsqlConnection()
		}
	}
	namespace System {
		class FuncᐸIDbConnectionᐳ {
				<<delegate>>
		}
	}
```

