/*
$v=true
$p=11
$d=Disposable singleton
$sa=Async disposable singleton
$sa=Tracking disposable instances per a composition root
$h=To dispose all created singleton instances, simply dispose the composition instance. The composition itself becomes disposable as soon as it can create at least one disposable singleton, so `using` around it is the whole disposal strategy.
$f=A composition class becomes disposable if it creates at least one disposable singleton instance.
$f=
$f=This is the disposal path for process-wide resources a server or a desktop shell owns: message brokers, hardware handles, file writers, caches with a background timer. They are created lazily on first use and released when the host shuts the composition down.
$f=>[!WARNING]
$f=>Only what the composition created is disposed, and only in a reverse order of creation. Transient and `PerResolve` instances, and objects built up by a builder, are not in that list and stay the responsibility of their owner.
$f=
$f=Exceptions thrown by one `Dispose` are caught and passed to the `OnDisposeException` partial method, so one faulty resource does not stop the rest. Override it to log, otherwise the failure is silent.
$f=Prefer `IAsyncDisposable` (see [Async disposable singleton](async-disposable-singleton.md)) when a release must await I/O; a synchronous `Dispose` that blocks hides the cost in shutdown.
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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // Disable Resolve methods to keep the public API minimal
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