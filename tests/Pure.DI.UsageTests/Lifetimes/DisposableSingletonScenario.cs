/*
$v=true
$p=7
$d=Disposable singleton
$h=To dispose all created singleton instances, simply dispose the composition instance:
$f=A composition class becomes disposable if it creates at least one disposable singleton instance.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Lifetimes.DisposableSingletonScenario;

using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
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
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")

            // A realistic example:
            // a submarine has a shared hardware bus to onboard sensors.
            // It should be created once and disposed when the "mission scope"
            // (the composition instance) ends.
            .Bind().As(Singleton).To<AcousticSensorBus>()
            .Bind().To<SubmarineCombatSystem>()
            .Root<ICombatSystem>("CombatSystem");

        IAcousticSensorBus bus;
        using (var composition = new Composition())
        {
            var combatSystem = composition.CombatSystem;

            // Store the singleton instance to verify that it gets disposed
            // when composition is disposed.
            bus = combatSystem.SensorBus;

            // In real usage you would call methods like:
            // combatSystem.ScanForContacts();
        }

        // When the mission scope ends, all disposable singletons created by it
        // must be disposed.
        bus.IsDisposed.ShouldBeTrue();
// }
        new Composition().SaveClassDiagram();
    }
}

// {
interface IAcousticSensorBus
{
    bool IsDisposed { get; }
}

// Represents a shared connection to submarine sensors (sonar, hydrophones, etc.).
// This is a singleton because the hardware bus is typically a single shared resource,
// and it must be cleaned up properly.
class AcousticSensorBus : IAcousticSensorBus, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface ICombatSystem
{
    IAcousticSensorBus SensorBus { get; }
}

// A "combat system" is a typical high-level service that uses shared hardware resources.
class SubmarineCombatSystem(IAcousticSensorBus sensorBus) : ICombatSystem
{
    public IAcousticSensorBus SensorBus { get; } = sensorBus;
}
// }