#### ThreadSafe hint

Hints are used to fine-tune code generation. The `ThreadSafe` hint determines whether object composition will be created in a thread-safe manner. This hint is `On` by default. It is good practice not to use threads when creating an object graph, in which case this hint can be turned off, which will lead to a slight increase in performance.
In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// ThreadSafe = Off`.


```c#
using Pure.DI;
using static Pure.DI.Hint;

DI.Setup(nameof(Composition))
    // Disabling thread-safety can improve performance.
    // This is safe when the object graph is resolved on a single thread,
    // for example at application startup.
    .Hint(ThreadSafe, "Off")
    .Bind().To<SqlDatabaseConnection>()
    .Bind().As(Lifetime.Singleton).To<ReportGenerator>()
    .Root<IReportGenerator>("Generator");

var composition = new Composition();
var reportGenerator = composition.Generator;

interface IDatabaseConnection;

class SqlDatabaseConnection : IDatabaseConnection;

interface IReportGenerator;

class ReportGenerator(Func<IDatabaseConnection> connectionFactory) : IReportGenerator;
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
- Add a reference to the NuGet package
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
```bash
dotnet add package Pure.DI
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

For more hints, see [this](../README.md#setup-hints) page.

The following partial class will be generated:

```c#
partial class Composition
{

  private ReportGenerator? _singletonReportGenerator63;

  public IReportGenerator Generator
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_singletonReportGenerator63 is null)
      {
        Func<IDatabaseConnection> perBlockFunc562 = new Func<IDatabaseConnection>(
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        () =>
        {
          return new SqlDatabaseConnection();
        });
        _singletonReportGenerator63 = new ReportGenerator(perBlockFunc562);
      }

      return _singletonReportGenerator63;
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
	SqlDatabaseConnection --|> IDatabaseConnection
	ReportGenerator --|> IReportGenerator
	Composition ..> ReportGenerator : IReportGenerator Generator
	ReportGenerator o-- "PerBlock" FuncᐸIDatabaseConnectionᐳ : FuncᐸIDatabaseConnectionᐳ
	FuncᐸIDatabaseConnectionᐳ *--  SqlDatabaseConnection : IDatabaseConnection
	namespace Pure.DI.UsageTests.Hints.ThreadSafeHintScenario {
		class Composition {
		<<partial>>
		+IReportGenerator Generator
		}
		class IDatabaseConnection {
			<<interface>>
		}
		class IReportGenerator {
			<<interface>>
		}
		class ReportGenerator {
				<<class>>
			+ReportGenerator(FuncᐸIDatabaseConnectionᐳ connectionFactory)
		}
		class SqlDatabaseConnection {
				<<class>>
			+SqlDatabaseConnection()
		}
	}
	namespace System {
		class FuncᐸIDatabaseConnectionᐳ {
				<<delegate>>
		}
	}
```

