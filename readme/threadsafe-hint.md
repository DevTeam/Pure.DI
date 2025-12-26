#### ThreadSafe hint

Hints are used to fine-tune code generation. The _ThreadSafe_ hint determines whether object composition will be created in a thread-safe manner. This hint is _On_ by default. It is good practice not to use threads when creating an object graph, in which case this hint can be turned off, which will lead to a slight increase in performance.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// ThreadSafe = Off`.


```c#
using Pure.DI;
using static Pure.DI.Hint;

DI.Setup(nameof(Composition))
    // Отключение потокобезопасности в композиции может повысить производительность.
    // Это безопасно, если граф объектов разрешается в одном потоке,
    // например, при запуске приложения.
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

- Make sure you have the [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later is installed
```bash
dotnet --list-sdk
```
- Create a net10.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add reference to NuGet package
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

For more hints, see [this](README.md#setup-hints) page.

The following partial class will be generated:

```c#
partial class Composition
{

  private ReportGenerator? _singletonReportGenerator52;

  public IReportGenerator Generator
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_singletonReportGenerator52 is null)
      {
        Func<IDatabaseConnection> transientFunc1 = new Func<IDatabaseConnection>(
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        () =>
        {
          IDatabaseConnection localValue30 = new SqlDatabaseConnection();
          return localValue30;
        });
        _singletonReportGenerator52 = new ReportGenerator(transientFunc1);
      }

      return _singletonReportGenerator52;
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

