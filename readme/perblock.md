#### PerBlock

The `PerBlock` lifetime reuses an instance inside a generated construction block. It is useful when several constructor parameters in the same object graph need the same expensive helper, but keeping that helper for the whole root (`PerResolve`) or composition (`Singleton`) would be too broad.
The order repository below receives the same database connection for both primary and secondary constructor paths within one block, then receives a fresh connection for the next root call. This reduces duplicate construction while keeping request-like operations isolated.


```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // Bind DatabaseConnection with PerBlock lifetime:
    // Ensures a single connection per composition root (e.g., per user request),
    // but a new one for each new root - useful for batch operations without full singleton overhead.
    .Bind().As(PerBlock).To<DatabaseConnection>()
    // Bind a tuple of two connections as Singleton:
    // This shares the same connection globally, simulating a cached or shared resource.
    .Bind().As(Singleton).To<(IDatabaseConnection conn3, IDatabaseConnection conn4)>()

    // Composition root - represents the main service entry point.
    .Root<OrderRepository>("Repository");

var composition = new Composition();

// Simulate the first user request or batch operation
var repository1 = composition.Repository;
repository1.ProcessOrder("ORD-2025-54546");

// Check that within one repository (one block), connections are shared for consistency
repository1.PrimaryConnection.ShouldBe(repository1.SecondaryConnection);
repository1.OtherConnection.ShouldBe(repository1.FallbackConnection);

repository1.PrimaryConnection.ShouldNotBe(repository1.OtherConnection);

// Simulate the second user request or batch - should have a new PerBlock connection
var repository2 = composition.Repository;
repository2.PrimaryConnection.ShouldNotBe(repository1.PrimaryConnection);

// Interface for database connection - in a real world, this could handle SQL queries
interface IDatabaseConnection;

// Implementation of database connection - transient-like but controlled by lifetime
class DatabaseConnection : IDatabaseConnection;

// Repository for handling orders, injecting multiple connections for demonstration
// In real-world, this could process orders in a batch, sharing connection within the batch
class OrderRepository(
    IDatabaseConnection primaryConnection,
    IDatabaseConnection secondaryConnection,
    (IDatabaseConnection otherConnection, IDatabaseConnection fallbackConnection) additionalConnections)
{
    // Public properties for connections - in practice, these would be private and used in methods
    public IDatabaseConnection PrimaryConnection { get; } = primaryConnection;

    public IDatabaseConnection SecondaryConnection { get; } = secondaryConnection;

    public IDatabaseConnection OtherConnection { get; } = additionalConnections.otherConnection;

    public IDatabaseConnection FallbackConnection { get; } = additionalConnections.fallbackConnection;

    // Example real-world method: Process an order using the shared connection
    public void ProcessOrder(string orderId)
    {
        // Use PrimaryConnection to query database, e.g.,
        // "SELECT * FROM Orders WHERE Id = @orderId"
    }
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
>`PerBlock` provides a balance between `Transient` and `PerResolve`: fewer allocations inside a local block without turning the dependency into long-lived shared state.
Use it for short-lived helpers, local adapters, and operation-level collaborators. Prefer `Scoped` or `PerResolve` when the reuse boundary must be visible at the application level.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
#if NET9_0_OR_GREATER
  private readonly Lock _lock = new Lock();
#else
  private readonly Object _lock = new Object();
#endif

  private (IDatabaseConnection conn3, IDatabaseConnection conn4) _singletonValueTuple;
  private bool _singletonValueTupleCreated;

  public OrderRepository Repository
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var perBlockDatabaseConnection = new DatabaseConnection();
      if (!_singletonValueTupleCreated)
        lock (_lock)
          if (!_singletonValueTupleCreated)
          {
            var perBlockDatabaseConnection1 = new DatabaseConnection();
            _singletonValueTuple = (perBlockDatabaseConnection1, perBlockDatabaseConnection1);
            Thread.MemoryBarrier();
            _singletonValueTupleCreated = true;
          }

      return new OrderRepository(perBlockDatabaseConnection, perBlockDatabaseConnection, _singletonValueTuple);
    }
  }
}
```

</details>

Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	DatabaseConnection --|> IDatabaseConnection
	ValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳ --|> IStructuralComparable
	ValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳ --|> IStructuralEquatable
	ValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳ --|> IComparable
	ValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳ --|> IComparableᐸValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳᐳ
	ValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳ --|> IEquatableᐸValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳᐳ
	ValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳ --|> ITuple
	Composition ..> OrderRepository : OrderRepository Repository
	OrderRepository o-- "2 PerBlock instances" DatabaseConnection : IDatabaseConnection
	OrderRepository o-- "Singleton" ValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳ : ValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳ
	ValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳ o-- "2 PerBlock instances" DatabaseConnection : IDatabaseConnection
	namespace Pure.DI.UsageTests.Lifetimes.PerBlockScenario {
		class Composition {
		<<partial>>
		+OrderRepository Repository
		}
		class DatabaseConnection {
				<<class>>
			+DatabaseConnection()
		}
		class IDatabaseConnection {
			<<interface>>
		}
		class OrderRepository {
				<<class>>
			+OrderRepository(IDatabaseConnection primaryConnection, IDatabaseConnection secondaryConnection, ValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳ additionalConnections)
		}
	}
	namespace System {
		class IComparable {
			<<interface>>
		}
		class IComparableᐸValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳᐳ {
			<<interface>>
		}
		class IEquatableᐸValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳᐳ {
			<<interface>>
		}
		class ValueTupleᐸIDatabaseConnectionˏIDatabaseConnectionᐳ {
				<<tuple>>
			+ValueTuple(IDatabaseConnection item1, IDatabaseConnection item2)
		}
	}
	namespace System.Collections {
		class IStructuralComparable {
			<<interface>>
		}
		class IStructuralEquatable {
			<<interface>>
		}
	}
	namespace System.Runtime.CompilerServices {
		class ITuple {
			<<interface>>
		}
	}
```

See also:

- [PerResolve](perresolve.md)
- [Transient](transient.md)

