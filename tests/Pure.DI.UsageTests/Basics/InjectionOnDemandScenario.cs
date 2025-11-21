/*
$v=true
$p=3
$d=Injection on demand
$h=This example demonstrates using dependency injection with Pure.DI to dynamically create dependencies as needed via a factory function. The code defines a service (`GameLevel`) that requires multiple instances of a dependency (`Enemy`). Instead of injecting pre-created instances, the service receives a `Func<IEnemy>` factory delegate, allowing it to generate entities on demand.
$f=Key elements:
$f=- `Enemy` is bound to the `IEnemy` interface, and `GameLevel` is bound to `IGameLevel`.
$f=- The `GameLevel` constructor accepts `Func<IEnemy>`, enabling deferred creation of entities.
$f=- The `GameLevel` calls the factory twice, resulting in two distinct `Enemy` instances stored in its `Enemies` collection.
$f=
$f=This approach showcases how factories can control dependency lifetime and instance creation timing in a DI container. The Pure.DI configuration ensures the factory resolves new `IEnemy` instances each time it's invoked, achieving "injections as required" functionality.
$r=Shouldly
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
        // This hint indicates to not generate methods such as Resolve
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
    // The factory acts as a "spawner" to create new enemy instances on demand.
    // Calling 'enemySpawner()' creates a fresh instance of Enemy each time.
    public IReadOnlyList<IEnemy> Enemies { get; } =
    [
        enemySpawner(),
        enemySpawner()
    ];
}
// }