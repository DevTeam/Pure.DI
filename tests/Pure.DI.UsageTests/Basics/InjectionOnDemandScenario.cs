/*
$v=true
$p=3
$d=Injection on demand
$h=This example creates dependencies on demand using a factory delegate. The service (`GameLevel`) needs multiple instances of `IEnemy`, so it receives a `Func<IEnemy>` that can create new instances when needed.
$h=This approach is useful when instances are created lazily or repeatedly during business execution.
$f=Key elements:
$f=- `Enemy` is bound to the `IEnemy` interface, and `GameLevel` is bound to `IGameLevel`.
$f=- The `GameLevel` constructor accepts `Func<IEnemy>`, enabling deferred creation of entities.
$f=- The `GameLevel` calls the factory twice, resulting in two distinct `Enemy` instances stored in its `Enemies` collection.
$f=
$f=This approach lets factories control lifetime and instantiation timing. Pure.DI resolves a new `IEnemy` each time the factory is invoked.
$r=Shouldly
$f=Limitations: factory delegate calls can create many objects, so lifetime choices still matter for performance and state.
$f=Common pitfalls:
$f=- Assuming `Func<T>` always returns new instances regardless of configured lifetime.
$f=- Hiding expensive work behind repeated on-demand calls.
$f=See also: [Injections on demand with arguments](injections-on-demand-with-arguments.md), [Func<T>](func.md).
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.InjectionOnDemandScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Generic;
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
            .Bind().To<Enemy>()
            .Bind().To<GameLevel>()

            // Composition root
            .Root<IGameLevel>("GameLevel");

        var composition = new Composition();
        var gameLevel = composition.GameLevel;

        // Verifies that two distinct enemies have been spawned
        gameLevel.Enemies.Count.ShouldBe(2);
// }
        composition.SaveClassDiagram();
    }
}

// {
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
// }
