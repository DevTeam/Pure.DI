/*
$v=true
$p=9
$d=Default lifetime for a type
$sa=Default lifetime
$sa=Default lifetime for a type and a tag
$h=For example, if a certain lifetime is used more often than others, you can make it the default lifetime for a certain type:
$h=`DefaultLifetime<T>(...)` applies to bindings of the contract `T` only, so the rest of the composition keeps the ordinary defaults. Here the station-wide clock is a singleton while the controller that uses it stays transient.
$f=>[!NOTE]
$f=>Setting default lifetime for types simplifies configuration when the same lifetime is consistently applied.
$f=
$f=This is the safer variant of a default lifetime, because it follows a contract rather than a position in the setup chain. Infrastructure contracts - a clock, a configuration reader, a telemetry sink - are the natural candidates for a `Singleton` rule stated once for the whole project.
$f=The rule is keyed by contract, not by implementation: every binding of `T` that does not set a lifetime explicity inherits it. Two implementations of the same contract therefore share the rule, which is usually what is wanted but must be checked when a transient implementation is registered later.
$f=Prefer `DefaultLifetime<T>(lifetime, tag)` (see [Default lifetime for a type and a tag](default-lifetime-for-a-type-and-a-tag.md)) when only one tagged registration should be shared.
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
        // Disable Resolve methods to keep the public API minimal
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