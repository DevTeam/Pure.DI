#### Simplified factory

This example shows a simplified manual factory. Each lambda parameter represents an injected dependency, and starting with C# 10 you can add `Tag(...)` to specify a tagged dependency.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind("today").To(() => DateTime.Today)
    // Injects FileLogger and DateTime
    // and applies additional initialization logic
    .Bind<IFileLogger>().To((
        FileLogger logger,
        [Tag("today")] DateTime date) => {
        logger.Init($"app-{date:yyyy-MM-dd}.log");
        return logger;
    })
    .Bind().To<OrderProcessingService>()

    // Composition root
    .Root<IOrderProcessingService>("OrderService");

var composition = new Composition();
var service = composition.OrderService;

service.Logger.FileName.ShouldBe($"app-{DateTime.Today:yyyy-MM-dd}.log");

interface IFileLogger
{
    string FileName { get; }

    void Log(string message);
}

class FileLogger : IFileLogger
{
    public string FileName { get; private set; } = "";

    public void Init(string fileName) => FileName = fileName;

    public void Log(string message)
    {
        // Write to file
    }
}

interface IOrderProcessingService
{
    IFileLogger Logger { get; }
}

class OrderProcessingService(IFileLogger logger) : IOrderProcessingService
{
    public IFileLogger Logger { get; } = logger;
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

The example creates a service that depends on a logger initialized with a date-based file name.
This style keeps the setup concise while still allowing explicit initialization logic.
The `Tag` attribute enables named dependencies for more complex setups.
Limitations: compact lambda factories stay readable only while initialization logic remains small.
Common pitfalls:
- Putting heavy imperative setup code into short lambda factories.
- Forgetting explicit tags when several same-type dependencies exist.
See also: [Factory](factory.md), [Tags](tags.md).

The following partial class will be generated:

```c#
partial class Composition
{
  public IOrderProcessingService OrderService
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      FileLogger transientFileLogger346;
      FileLogger localLogger4 = new FileLogger();
      DateTime transientDateTime348 = DateTime.Today;
      DateTime localDate = transientDateTime348;
      localLogger4.Init($"app-{localDate:yyyy-MM-dd}.log");
      transientFileLogger346 = localLogger4;
      return new OrderProcessingService(transientFileLogger346);
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
	FileLogger --|> IFileLogger
	OrderProcessingService --|> IOrderProcessingService
	Composition ..> OrderProcessingService : IOrderProcessingService OrderService
	FileLogger *--  DateTime : "today"  DateTime
	OrderProcessingService *--  FileLogger : IFileLogger
	namespace Pure.DI.UsageTests.Basics.SimplifiedFactoryScenario {
		class Composition {
		<<partial>>
		+IOrderProcessingService OrderService
		}
		class FileLogger {
				<<class>>
		}
		class IFileLogger {
			<<interface>>
		}
		class IOrderProcessingService {
			<<interface>>
		}
		class OrderProcessingService {
				<<class>>
			+OrderProcessingService(IFileLogger logger)
		}
	}
	namespace System {
		class DateTime {
				<<struct>>
		}
	}
```

