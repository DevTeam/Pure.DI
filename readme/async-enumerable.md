#### Async Enumerable

Specifying `IAsyncEnumerable<T>` as the injection type allows instances of all bindings implementing type `T` to be injected in an asynchronous-lazy manner - the instances will be provided one at a time, in an order corresponding to the sequence of the bindings.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IHealthCheck>().To<MemoryCheck>()
    .Bind<IHealthCheck>("External").To<ExternalServiceCheck>()
    .Bind<IHealthService>().To<HealthService>()

    // Composition root
    .Root<IHealthService>("HealthService");

var composition = new Composition();
var healthService = composition.HealthService;
var checks = await healthService.GetChecksAsync();

checks[0].ShouldBeOfType<MemoryCheck>();
checks[1].ShouldBeOfType<ExternalServiceCheck>();

interface IHealthCheck;

class MemoryCheck : IHealthCheck;

class ExternalServiceCheck : IHealthCheck;

interface IHealthService
{
    Task<IReadOnlyList<IHealthCheck>> GetChecksAsync();
}

class HealthService(IAsyncEnumerable<IHealthCheck> checks) : IHealthService
{
    public async Task<IReadOnlyList<IHealthCheck>> GetChecksAsync()
    {
        var results = new List<IHealthCheck>();
        await foreach (var check in checks)
        {
            results.Add(check);
        }

        return results;
    }
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
  [OrdinalAttribute(256)]
  public Composition()
  {
  }

  internal Composition(Composition parentScope)
  {
  }

  public IHealthService HealthService
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      async IAsyncEnumerable<IHealthCheck> EnumerationOf_transientIAsyncEnumerable1()
      {
        yield return new MemoryCheck();
        yield return new ExternalServiceCheck();
        await Task.CompletedTask;
      }

      return new HealthService(EnumerationOf_transientIAsyncEnumerable1());
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
	MemoryCheck --|> IHealthCheck
	ExternalServiceCheck --|> IHealthCheck : "External" 
	HealthService --|> IHealthService
	Composition ..> HealthService : IHealthService HealthService
	HealthService *--  IAsyncEnumerableᐸIHealthCheckᐳ : IAsyncEnumerableᐸIHealthCheckᐳ
	IAsyncEnumerableᐸIHealthCheckᐳ *--  MemoryCheck : IHealthCheck
	IAsyncEnumerableᐸIHealthCheckᐳ *--  ExternalServiceCheck : "External"  IHealthCheck
	namespace Pure.DI.UsageTests.BCL.AsyncEnumerableScenario {
		class Composition {
		<<partial>>
		+IHealthService HealthService
		}
		class ExternalServiceCheck {
				<<class>>
			+ExternalServiceCheck()
		}
		class HealthService {
				<<class>>
			+HealthService(IAsyncEnumerableᐸIHealthCheckᐳ checks)
		}
		class IHealthCheck {
			<<interface>>
		}
		class IHealthService {
			<<interface>>
		}
		class MemoryCheck {
				<<class>>
			+MemoryCheck()
		}
	}
	namespace System.Collections.Generic {
		class IAsyncEnumerableᐸIHealthCheckᐳ {
				<<interface>>
		}
	}
```

