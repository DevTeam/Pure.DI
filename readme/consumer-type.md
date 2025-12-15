#### Consumer type

`ConsumerType` is used to get the consumer type of the given dependency. The use of `ConsumerType` is demonstrated on the example of [Serilog library](https://serilog.net/):


```c#
using Shouldly;
using Serilog.Core;
using Serilog.Events;
using Pure.DI;
using Serilog.Core;

Serilog.ILogger serilogLogger = new Serilog.LoggerConfiguration().CreateLogger();
var composition = new Composition(logger: serilogLogger);
var service = composition.Root;

interface IDependency;

class Dependency : IDependency
{
    public Dependency(Serilog.ILogger log)
    {
        log.Information("Dependency created");
    }
}

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(
        Serilog.ILogger log,
        IDependency dependency)
    {
        Dependency = dependency;
        log.Information("Service initialized");
    }

    public IDependency Dependency { get; }
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Arg<Serilog.ILogger>("logger", "from arg")
            .Bind().To(ctx => {
                ctx.Inject<Serilog.ILogger>("from arg", out var logger);

                // Enriches the logger with the specific context of the consumer type.
                // ctx.ConsumerType represents the type into which the dependency is being injected.
                // This allows logs to be tagged with the name of the class that created them.
                return logger.ForContext(ctx.ConsumerType);
            })
            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<IService>(nameof(Root));
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
  - [Serilog.Core](https://www.nuget.org/packages/Serilog.Core)
  - [Serilog.Events](https://www.nuget.org/packages/Serilog.Events)
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
dotnet add package Serilog.Core
dotnet add package Serilog.Events
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

  private readonly Serilog.ILogger _argLogger;

  [OrdinalAttribute(128)]
  public Composition(Serilog.ILogger logger)
  {
    _argLogger = logger ?? throw new ArgumentNullException(nameof(logger));
#if NET9_0_OR_GREATER
    _lock = new Lock();
#else
    _lock = new Object();
#endif
  }

  internal Composition(Composition parentScope)
  {
    _argLogger = parentScope._argLogger;
    _lock = parentScope._lock;
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Serilog.ILogger transientILogger3;
      Serilog.ILogger localLogger2 = _argLogger;
      // Enriches the logger with the specific context of the consumer type.
      // ctx.ConsumerType represents the type into which the dependency is being injected.
      // This allows logs to be tagged with the name of the class that created them.
      transientILogger3 = localLogger2.ForContext(typeof(Dependency));
      Serilog.ILogger transientILogger1;
      Serilog.ILogger localLogger3 = _argLogger;
      // Enriches the logger with the specific context of the consumer type.
      // ctx.ConsumerType represents the type into which the dependency is being injected.
      // This allows logs to be tagged with the name of the class that created them.
      transientILogger1 = localLogger3.ForContext(typeof(Service));
      return new Service(transientILogger1, new Dependency(transientILogger3));
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
	Dependency --|> IDependency
	Service --|> IService
	Composition ..> Service : IService Root
	ILogger o-- ILogger : "from arg"  Argument "logger"
	Dependency *--  ILogger : ILogger
	Service *--  ILogger : ILogger
	Service *--  Dependency : IDependency
	namespace Pure.DI.UsageTests.Basics.ConsumerTypeScenario {
		class Composition {
		<<partial>>
		+IService Root
		}
		class Dependency {
				<<class>>
			+Dependency(ILogger log)
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
				<<class>>
			+Service(ILogger log, IDependency dependency)
		}
	}
	namespace Serilog {
		class ILogger {
				<<interface>>
		}
	}
```

