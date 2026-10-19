/*
$v=true
$p=21
$d=Builder
$sa=Build up of an existing object
$sa=Builder with arguments
$sa=Builders
$h=Sometimes you need to build up an existing composition root and inject all of its dependencies, in which case the `Builder` method will be useful, as in the example below:
$f=Build-up is for objects someone else creates: a UI framework instantiating a window or a view model, a game engine spawning an entity, a deserializer that calls the parameterless constructor.
$f=The generated method name is whatever you pass (`BuildUp` by default) and its first parameter is always the instance to be built, followed by the root arguments in the order they are declared.
$f=Only members marked `[Dependency]` or `[Ordinal]` are injected, in ordinal order, so a plain auto-property is left untouched and the framework's own values are preserved.
$f=The instance is mutated in place, which means the container takes no ownership: dispose and lifetime of that object stay with the caller.
$f=Prefer constructor injection for objects the composition creates, and keep a builder for the boundary where an external framework owns the lifetime.
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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
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