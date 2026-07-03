#### Builder

Sometimes you need to build up an existing composition root and inject all of its dependencies, in which case the `Builder` method will be useful, as in the example below:


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To(Guid.NewGuid)
    .Bind().To<PhotonBlaster>()
    .Builder<Player>("Equip");

var composition = new Composition();

// The Game Engine instantiates the Player entity,
// so we need to inject dependencies into the existing instance.
var player = composition.Equip(new Player());

player.Id.ShouldNotBe(Guid.Empty);
player.Weapon.ShouldBeOfType<PhotonBlaster>();

interface IWeapon;

class PhotonBlaster : IWeapon;

interface IGameEntity
{
    Guid Id { get; }

    IWeapon? Weapon { get; }
}

record Player : IGameEntity
{
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IWeapon? Weapon { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
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

Advantages:
- Allows working with pre-existing objects
- Provides flexibility in dependency injection
- Supports partial injection of dependencies
- Can be used with legacy code

Use Cases:
- When objects are created outside the DI
- For working with third-party libraries
- When migrating existing code to DI
- For complex object graphs where full construction is not feasible

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Player Equip(Player buildingInstance)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    Player transientPlayer;
    Player localBuildingInstance = buildingInstance;
    Guid transientGuid = Guid.NewGuid();
    localBuildingInstance.Weapon = new PhotonBlaster();
    localBuildingInstance.SetId(transientGuid);
    transientPlayer = localBuildingInstance;
    return transientPlayer;
  }
}
```

</details>

Class diagram:

```mermaid
---
 config:
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
	PhotonBlaster --|> IWeapon
	Composition ..> Player : Player Equip(Pure.DI.UsageTests.Basics.BuilderScenario.Player buildingInstance)
	Player *-- Guid : Guid
	Player *-- PhotonBlaster : IWeaponɁ
	namespace Pure.DI.UsageTests.Basics.BuilderScenario {
		class Composition {
		<<partial>>
		+Player Equip(Pure.DI.UsageTests.Basics.BuilderScenario.Player buildingInstance)
		}
		class IWeapon {
			<<interface>>
		}
		class PhotonBlaster {
				<<class>>
			+PhotonBlaster()
		}
		class Player {
				<<record>>
			+IWeaponɁ Weapon
			+SetId(Guid id) : Void
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

See also:

- [Build up of an existing object](build-up-of-an-existing-object.md)
- [Builder with arguments](builder-with-arguments.md)
- [Builders](builders.md)

