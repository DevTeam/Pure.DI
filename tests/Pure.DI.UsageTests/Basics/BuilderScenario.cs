/*
$v=true
$p=9
$d=Builder
$h=Sometimes you need to build up an existing composition root and inject all of its dependencies, in which case the `Builder` method will be useful, as in the example below:
$f=Important Notes:
$f=- The default builder method name is `BuildUp`
$f=- The first argument to the builder method is always the instance to be built
$f=
$f=Advantages:
$f=- Allows working with pre-existing objects
$f=- Provides flexibility in dependency injection
$f=- Supports partial injection of dependencies
$f=- Can be used with legacy code
$f=
$f=Use Cases:
$f=- When objects are created outside the DI container
$f=- For working with third-party libraries
$f=- When migrating existing code to DI
$f=- For complex object graphs where full construction is not feasible
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI.UsageTests.Basics.BuilderScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind().To(_ => Guid.NewGuid())
            .Bind().To<PhotonBlaster>()
            .Builder<Player>("Equip");

        var composition = new Composition();

        // The Game Engine instantiates the Player entity,
        // so we need to inject dependencies into the existing instance.
        var player = composition.Equip(new Player());

        player.Id.ShouldNotBe(Guid.Empty);
        player.Weapon.ShouldBeOfType<PhotonBlaster>();
// }
        composition.SaveClassDiagram();
    }
}

// {
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
// }