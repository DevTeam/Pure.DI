/*
$v=true
$p=6
$d=Default lifetime for a type
$h=For example, if a certain lifetime is used more often than others, you can make it the default lifetime for a certain type:
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageTests.Lifetimes.DefaultLifetimeForTypeScenario;

using Shouldly;
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
            // In a real base station, the time source (PTP/GNSS disciplined clock)
            // is a shared infrastructure component:
            // it should be created once per station and reused everywhere.
            .DefaultLifetime<ITimeSource>(Singleton)

            // Time source used by multiple subsystems
            .Bind().To<GnssTimeSource>()

            // Upper-level station components (usually transient by default)
            .Bind().To<BaseStationController>()
            .Bind().To<RadioScheduler>()

            // Composition root (represents "get me a controller instance")
            .Root<IBaseStationController>("Controller");

        var composition = new Composition();

        // Two independent controller instances (e.g., two independent operations)
        var controller1 = composition.Controller;
        var controller2 = composition.Controller;

        controller1.ShouldNotBe(controller2);

        // Inside one controller we request ITimeSource twice:
        // the same singleton instance should be injected both times.
        controller1.SyncTimeSource.ShouldBe(controller1.SchedulerTimeSource);

        // Across different controllers the same station-wide time source is reused.
        controller1.SyncTimeSource.ShouldBe(controller2.SyncTimeSource);
// }
        composition.SaveClassDiagram();
    }
}

// {
// A shared station-wide dependency
interface ITimeSource
{
    long UnixTimeMilliseconds { get; }
}

// Represents a GNSS-disciplined clock (or PTP grandmaster input).
// In real deployments you'd talk to a driver / NIC / daemon here.
class GnssTimeSource : ITimeSource
{
    public long UnixTimeMilliseconds => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

interface IBaseStationController
{
    ITimeSource SyncTimeSource { get; }
    ITimeSource SchedulerTimeSource { get; }
}

// A "top-level" controller of the base station.
// It depends on the time source for synchronization and for scheduling decisions.
class BaseStationController(
    ITimeSource syncTimeSource,
    RadioScheduler scheduler)
    : IBaseStationController
{
    // Used for time synchronization / frame timing
    public ITimeSource SyncTimeSource { get; } = syncTimeSource;

    // Demonstrates that scheduler also uses the same singleton time source
    public ITimeSource SchedulerTimeSource { get; } = scheduler.TimeSource;
}

// A subsystem (e.g., MAC scheduler) that also needs precise time.
class RadioScheduler(ITimeSource timeSource)
{
    public ITimeSource TimeSource { get; } = timeSource;
}
// }