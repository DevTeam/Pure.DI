#### Builder with arguments

This example demonstrates how to use builders with custom arguments in dependency injection. It shows how to pass additional parameters during the build-up process.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<Guid>("id")
    .Bind().To<TelemetrySystem>()
    .Builder<Satellite>("Initialize");

var composition = new Composition();

var id = Guid.NewGuid();
var satellite = composition.Initialize(new Satellite(), id);
satellite.Id.ShouldBe(id);
satellite.Telemetry.ShouldBeOfType<TelemetrySystem>();

interface ITelemetrySystem;

class TelemetrySystem : ITelemetrySystem;

interface ISatellite
{
    Guid Id { get; }

    ITelemetrySystem? Telemetry { get; }
}

record Satellite : ISatellite
{
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public ITelemetrySystem? Telemetry { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
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

Important Notes:
- The default builder method name is `BuildUp`
- The first argument to the builder method is always the instance to be built
- Additional arguments are passed in the order they are defined in the setup
- Root arguments can be used to provide custom values during build-up

Use Cases:
- When additional parameters are required during object construction
- For scenarios where dependencies depend on runtime values
- When specific initialization data is needed
- For conditional injection based on provided arguments

Best Practices
- Keep the number of builder arguments minimal
- Use meaningful names for root arguments

The following partial class will be generated:

```c#
partial class Composition
{
#if NET9_0_OR_GREATER
  private readonly Lock _lock = new Lock();
#else
  private readonly Object _lock = new Object();
#endif

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Satellite Initialize(Satellite buildingInstance, Guid id)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    Satellite transientSatellite;
    Satellite localBuildingInstance7 = buildingInstance;
    localBuildingInstance7.Telemetry = new TelemetrySystem();
    localBuildingInstance7.SetId(id);
    transientSatellite = localBuildingInstance7;
    return transientSatellite;
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
	TelemetrySystem --|> ITelemetrySystem
	Composition ..> Satellite : Satellite Initialize(Pure.DI.UsageTests.Basics.BuilderWithArgumentsScenario.Satellite buildingInstance, System.Guid id)
	Satellite o-- Guid : Argument "id"
	Satellite *--  TelemetrySystem : ITelemetrySystem
	namespace Pure.DI.UsageTests.Basics.BuilderWithArgumentsScenario {
		class Composition {
		<<partial>>
		+Satellite Initialize(Pure.DI.UsageTests.Basics.BuilderWithArgumentsScenario.Satellite buildingInstance, System.Guid id)
		}
		class ITelemetrySystem {
			<<interface>>
		}
		class Satellite {
				<<record>>
			+ITelemetrySystem Telemetry
			+SetId(Guid id) : Void
		}
		class TelemetrySystem {
				<<class>>
			+TelemetrySystem()
		}
	}
	namespace System {
		class Guid {
				<<struct>>
		}
	}
```

