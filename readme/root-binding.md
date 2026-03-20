#### Root binding

In general, it is recommended to define one composition root for the entire application. But Sometimes you need to have multiple roots. To simplify the definition of composition roots, a "hybrid" API method `RootBind<T>(string rootName)` was added. It lets you define a binding and at the same time the root of the composition. You can it in order to reduce repetitions. The registration `composition.RootBind<IDependency>().To<Dependency>()` is an equivalent to `composition.Bind<IDependency>().To<Dependency>().Root<IDependency>()`.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<DbConnection>()
    // RootBind lets you define a binding and a composition root
    // simultaneously. This is useful for creating public entry points
    // for your application components while keeping the configuration concise.
    .RootBind<IOrderService>("OrderService").To<OrderService>();

// The line above is functionally equivalent to:
//  .Bind<IOrderService>().To<OrderService>()
//  .Root<IOrderService>("OrderService")

var composition = new Composition();
var orderService = composition.OrderService;
orderService.ShouldBeOfType<OrderService>();

interface IDbConnection;

class DbConnection : IDbConnection;

interface IOrderService;

class OrderService(IDbConnection connection) : IOrderService;
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
>`RootBind` reduces boilerplate when you need both a binding and a root for the same type.

The following partial class will be generated:

```c#
partial class Composition
{
#if NET9_0_OR_GREATER
  private readonly Lock _lock = new Lock();
#else
  private readonly Object _lock = new Object();
#endif

  private DbConnection? _singletonDbConnection62;

  public IOrderService OrderService
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_singletonDbConnection62 is null)
        lock (_lock)
          if (_singletonDbConnection62 is null)
          {
            _singletonDbConnection62 = new DbConnection();
          }

      return new OrderService(_singletonDbConnection62);
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
	DbConnection --|> IDbConnection
	OrderService --|> IOrderService
	Composition ..> OrderService : IOrderService OrderService
	OrderService o-- "Singleton" DbConnection : IDbConnection
	namespace Pure.DI.UsageTests.Basics.RootBindScenario {
		class Composition {
		<<partial>>
		+IOrderService OrderService
		}
		class DbConnection {
				<<class>>
			+DbConnection()
		}
		class IDbConnection {
			<<interface>>
		}
		class IOrderService {
			<<interface>>
		}
		class OrderService {
				<<class>>
			+OrderService(IDbConnection connection)
		}
	}
```

