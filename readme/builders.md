#### Builders

Sometimes you need builders for all types derived from `T` that are known at compile time.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To(Guid.NewGuid)
    .Bind().To<PlutoniumBattery>()
    // Creates a builder for each type inherited from IRobot.
    // These types must be available at this point in the code.
    .Builders<IRobot>("BuildUp");

var composition = new Composition();

var cleaner = composition.BuildUp(new CleanerBot());
cleaner.Token.ShouldNotBe(Guid.Empty);
cleaner.Battery.ShouldBeOfType<PlutoniumBattery>();

var guard = composition.BuildUp(new GuardBot());
guard.Token.ShouldBe(Guid.Empty);
guard.Battery.ShouldBeOfType<PlutoniumBattery>();

// Uses a common method to build an instance
IRobot robot = new CleanerBot();
robot = composition.BuildUp(robot);
robot.ShouldBeOfType<CleanerBot>();
robot.Token.ShouldNotBe(Guid.Empty);
robot.Battery.ShouldBeOfType<PlutoniumBattery>();

interface IBattery;

class PlutoniumBattery : IBattery;

interface IRobot
{
    Guid Token { get; }

    IBattery? Battery { get; }
}

record CleanerBot : IRobot
{
    public Guid Token { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IBattery? Battery { get; set; }

    [Dependency]
    public void SetToken(Guid token) => Token = token;
}

record GuardBot : IRobot
{
    public Guid Token => Guid.Empty;

    [Dependency]
    public IBattery? Battery { get; set; }
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

Important Notes:
- The default builder method name is `BuildUp`
- The first argument to the builder method is always the instance to be built

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
  public CleanerBot BuildUp(CleanerBot buildingInstance)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    CleanerBot transientCleanerBot250;
    CleanerBot localBuildingInstance3 = buildingInstance;
    Guid transientGuid253 = Guid.NewGuid();
    localBuildingInstance3.Battery = new PlutoniumBattery();
    localBuildingInstance3.SetToken(transientGuid253);
    transientCleanerBot250 = localBuildingInstance3;
    return transientCleanerBot250;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public GuardBot BuildUp(GuardBot buildingInstance)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    GuardBot transientGuardBot247;
    GuardBot localBuildingInstance2 = buildingInstance;
    localBuildingInstance2.Battery = new PlutoniumBattery();
    transientGuardBot247 = localBuildingInstance2;
    return transientGuardBot247;
  }

  #pragma warning disable CS0162
  [MethodImpl(MethodImplOptions.NoInlining)]
  public IRobot BuildUp(IRobot buildingInstance)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    switch (buildingInstance)
    {
      case CleanerBot CleanerBot:
        return BuildUp(CleanerBot);
      case GuardBot GuardBot:
        return BuildUp(GuardBot);
      default:
        throw new ArgumentException($"Unable to build an instance of typeof type {buildingInstance.GetType()}.", "buildingInstance");
    }
    return buildingInstance;
  }
  #pragma warning restore CS0162
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
	Guid --|> IComparable
	Guid --|> IComparableᐸGuidᐳ
	Guid --|> IEquatableᐸGuidᐳ
	Guid --|> IFormattable
	Guid --|> IParsableᐸGuidᐳ
	Guid --|> ISpanFormattable
	Guid --|> ISpanParsableᐸGuidᐳ
	Guid --|> IUtf8SpanFormattable
	Guid --|> IUtf8SpanParsableᐸGuidᐳ
	PlutoniumBattery --|> IBattery
	Composition ..> IRobot : IRobot BuildUp(Pure.DI.UsageTests.Basics.BuildersScenario.IRobot buildingInstance)
	Composition ..> GuardBot : GuardBot BuildUp(Pure.DI.UsageTests.Basics.BuildersScenario.GuardBot buildingInstance)
	Composition ..> CleanerBot : CleanerBot BuildUp(Pure.DI.UsageTests.Basics.BuildersScenario.CleanerBot buildingInstance)
	CleanerBot *--  Guid : Guid
	CleanerBot *--  PlutoniumBattery : IBattery
	GuardBot *--  PlutoniumBattery : IBattery
	namespace Pure.DI.UsageTests.Basics.BuildersScenario {
		class CleanerBot {
				<<record>>
			+IBattery Battery
			+SetToken(Guid token) : Void
		}
		class Composition {
		<<partial>>
		+CleanerBot BuildUp(Pure.DI.UsageTests.Basics.BuildersScenario.CleanerBot buildingInstance)
		+GuardBot BuildUp(Pure.DI.UsageTests.Basics.BuildersScenario.GuardBot buildingInstance)
		+IRobot BuildUp(Pure.DI.UsageTests.Basics.BuildersScenario.IRobot buildingInstance)
		}
		class GuardBot {
				<<record>>
			+IBattery Battery
		}
		class IBattery {
			<<interface>>
		}
		class IRobot {
				<<interface>>
		}
		class PlutoniumBattery {
				<<class>>
			+PlutoniumBattery()
		}
	}
	namespace System {
		class Guid {
				<<struct>>
		}
		class IComparable {
			<<interface>>
		}
		class IComparableᐸGuidᐳ {
			<<interface>>
		}
		class IEquatableᐸGuidᐳ {
			<<interface>>
		}
		class IFormattable {
			<<interface>>
		}
		class IParsableᐸGuidᐳ {
			<<interface>>
		}
		class ISpanFormattable {
			<<interface>>
		}
		class ISpanParsableᐸGuidᐳ {
			<<interface>>
		}
		class IUtf8SpanFormattable {
			<<interface>>
		}
		class IUtf8SpanParsableᐸGuidᐳ {
			<<interface>>
		}
	}
```

