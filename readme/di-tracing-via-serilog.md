#### DI tracing via serilog


```c#
using Serilog.Core;
using Serilog.Events;
using Pure.DI;
using System.Runtime.CompilerServices;

Serilog.ILogger serilogLogger = new Serilog.LoggerConfiguration().CreateLogger();
var composition = new Composition(logger: serilogLogger);
var service = composition.Root;

interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(Serilog.ILogger log, IDependency dependency)
    {
        Dependency = dependency;
        log.Information("Created");
    }

    public IDependency Dependency { get; }
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Hint(Hint.OnNewInstance, "On")
            .Hint(Hint.OnDependencyInjection, "On")
            // Excluding loggers
            .Hint(Hint.OnNewInstanceImplementationTypeNameRegularExpression, "^((?!Logger).)*$")
            .Hint(Hint.OnDependencyInjectionContractTypeNameRegularExpression, "^((?!Logger).)*$")

            .Arg<Serilog.ILogger>("logger", "from arg")
            .Bind<Serilog.ILogger>().To(ctx =>
            {
                ctx.Inject("from arg", out Serilog.ILogger logger);
                return logger.ForContext(ctx.ConsumerTypes[0]);
            })

            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<Serilog.ILogger>(nameof(Log), kind: RootKinds.Private)
            .Root<IService>(nameof(Root));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime) =>
        Log.Information("Created [{Value}], tag [{Tag}] as {Lifetime}", value, tag, lifetime);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)
    {
        Log.Information("Injected [{Value}], tag [{Tag}] as {Lifetime}", value, tag, lifetime);
        return value;
    }
}
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later is installed
```bash
dotnet --list-sdk
```
- Create a net9.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to NuGet packages
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
  - [Serilog.Core](https://www.nuget.org/packages/Serilog.Core)
  - [Serilog.Events](https://www.nuget.org/packages/Serilog.Events)
```bash
dotnet add package Pure.DI
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
  }

  private Serilog.ILogger Log
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Serilog.ILogger transILogger4;
      Serilog.ILogger localLogger1 = _argLogger;
      transILogger4 = localLogger1.ForContext(new Type[2] { typeof(Serilog.ILogger), typeof(Service) }[0]);
      return transILogger4;
    }
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var transDependency2 = new Dependency();
      OnNewInstance<Dependency>(ref transDependency2, null, Lifetime.Transient);
      Serilog.ILogger transILogger1;
      Serilog.ILogger localLogger = _argLogger;
      transILogger1 = localLogger.ForContext(new Type[2] { typeof(Serilog.ILogger), typeof(Service) }[0]);
      var transService = new Service(transILogger1, OnDependencyInjection<IDependency>(transDependency2, null, Lifetime.Transient));
      OnNewInstance<Service>(ref transService, null, Lifetime.Transient);
      return OnDependencyInjection<IService>(transService, null, Lifetime.Transient);
    }
  }


  partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime);

  private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime);
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
	Composition ..> ILogger : ILogger Log
	ILogger o-- ILogger : "from arg"  Argument "logger"
	Service *--  ILogger : ILogger
	Service *--  Dependency : IDependency
	namespace Pure.DI.UsageTests.Advanced.DITracingViaSerilogScenario {
		class Composition {
		<<partial>>
		-ILogger Log
		+IService Root
		}
		class Dependency {
				<<class>>
			+Dependency()
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

