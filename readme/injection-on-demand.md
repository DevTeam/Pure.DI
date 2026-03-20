#### Injection on demand

This example creates dependencies on demand using a factory delegate. The service (`GameLevel`) needs multiple instances of `IEnemy`, so it receives a `Func<IEnemy>` that can create new instances when needed.


```c#
using Shouldly;
using Pure.DI;
using System.Collections.Generic;

DI.Setup(nameof(Composition))
    .Bind().To<Enemy>()
    .Bind().To<GameLevel>()

    // Composition root
    .Root<IGameLevel>("GameLevel");

var composition = new Composition();
var gameLevel = composition.GameLevel;

// Verifies that two distinct enemies have been spawned
gameLevel.Enemies.Count.ShouldBe(2);

// Represents a game entity that acts as an enemy
interface IEnemy;

class Enemy : IEnemy;

// Represents a game level that manages entities
interface IGameLevel
{
    IReadOnlyList<IEnemy> Enemies { get; }
}

class GameLevel(Func<IEnemy> enemySpawner) : IGameLevel
{
    // The factory spawns a fresh enemy instance on each call.
    public IReadOnlyList<IEnemy> Enemies { get; } =
    [
        enemySpawner(),
        enemySpawner()
    ];
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

Key elements:
- `Enemy` is bound to the `IEnemy` interface, and `GameLevel` is bound to `IGameLevel`.
- The `GameLevel` constructor accepts `Func<IEnemy>`, enabling deferred creation of entities.
- The `GameLevel` calls the factory twice, resulting in two distinct `Enemy` instances stored in its `Enemies` collection.

This approach lets factories control lifetime and instantiation timing. Pure.DI resolves a new `IEnemy` each time the factory is invoked.

The following partial class will be generated:

```c#
partial class Composition
{
  public IGameLevel GameLevel
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Func<IEnemy> perBlockFunc296 = new Func<IEnemy>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
        return new Enemy();
      });
      return new GameLevel(perBlockFunc296);
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
	Enemy --|> IEnemy
	GameLevel --|> IGameLevel
	Composition ..> GameLevel : IGameLevel GameLevel
	GameLevel o-- "PerBlock" FuncᐸIEnemyᐳ : FuncᐸIEnemyᐳ
	FuncᐸIEnemyᐳ *--  Enemy : IEnemy
	namespace Pure.DI.UsageTests.Basics.InjectionOnDemandScenario {
		class Composition {
		<<partial>>
		+IGameLevel GameLevel
		}
		class Enemy {
				<<class>>
			+Enemy()
		}
		class GameLevel {
				<<class>>
			+GameLevel(FuncᐸIEnemyᐳ enemySpawner)
		}
		class IEnemy {
			<<interface>>
		}
		class IGameLevel {
			<<interface>>
		}
	}
	namespace System {
		class FuncᐸIEnemyᐳ {
				<<delegate>>
		}
	}
```

