#### Tuple

The tuples feature provides concise syntax to group multiple data elements in a lightweight data structure. The following example shows how a type can ask to inject a tuple argument into it:


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IEngine>().To<ElectricEngine>()
    .Bind<Coordinates>().To(() => new Coordinates(10, 20))
    .Bind<IVehicle>().To<Car>()

    // Composition root
    .Root<IVehicle>("Vehicle");

var composition = new Composition();
var vehicle = composition.Vehicle;

interface IEngine;

class ElectricEngine : IEngine;

readonly record struct Coordinates(int X, int Y);

interface IVehicle
{
    IEngine Engine { get; }
}

class Car((Coordinates StartPosition, IEngine Engine) specs) : IVehicle
{
    // The tuple 'specs' groups the initialization data (like coordinates)
    // and dependencies (like engine) into a single lightweight argument.
    public IEngine Engine { get; } = specs.Engine;
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
>Tuples are useful for returning multiple values from a method or grouping related dependencies without creating explicit types.

The following partial class will be generated:

```c#
partial class Composition
{
  public IVehicle Vehicle
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Coordinates transientCoordinates459 = new Coordinates(10, 20);
      return new Car((transientCoordinates459, new ElectricEngine()));
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
	ElectricEngine --|> IEngine
	Car --|> IVehicle
	Composition ..> Car : IVehicle Vehicle
	Car *--  ValueTupleᐸCoordinatesˏIEngineᐳ : ValueTupleᐸCoordinatesˏIEngineᐳ
	ValueTupleᐸCoordinatesˏIEngineᐳ *--  ElectricEngine : IEngine
	ValueTupleᐸCoordinatesˏIEngineᐳ *--  Coordinates : Coordinates
	namespace Pure.DI.UsageTests.BCL.TupleScenario {
		class Car {
				<<class>>
			+Car(ValueTupleᐸCoordinatesˏIEngineᐳ specs)
		}
		class Composition {
		<<partial>>
		+IVehicle Vehicle
		}
		class Coordinates {
				<<struct>>
		}
		class ElectricEngine {
				<<class>>
			+ElectricEngine()
		}
		class IEngine {
			<<interface>>
		}
		class IVehicle {
			<<interface>>
		}
	}
	namespace System {
		class ValueTupleᐸCoordinatesˏIEngineᐳ {
				<<struct>>
			+ValueTuple(Coordinates item1, IEngine item2)
		}
	}
```

