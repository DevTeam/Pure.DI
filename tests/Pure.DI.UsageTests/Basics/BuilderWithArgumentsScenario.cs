/*
$v=true
$p=22
$d=Builder with arguments
$sa=Builder
$h=This example shows how to use builders with custom arguments in dependency injection. It shows how to pass additional parameters during the build-up process.
$f=The builder signature is the instance first, then the root arguments in declaration order, so `Initialize(new Satellite(), id)` pairs `[Dependency]` members with values that are known only at that moment.
$f=Root arguments reach the whole graph of the build-up, not just the injected member, so a service created during build-up can receive the same value.
$f=The default builder method name is `BuildUp`; the first argument to the builder method is always the instance to be built.
$f=An argument of a non-nullable reference type is null-checked in the generated method and throws `ArgumentNullException` before anything is injected.
$f=Prefer few builder arguments with meaningful names: each one widens the builder signature and couples the host to the object's internals.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI.UsageTests.Basics.BuilderWithArgumentsScenario;

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
            .RootArg<Guid>("id")
            .Bind().To<TelemetrySystem>()
            .Builder<Satellite>("Initialize");

        var composition = new Composition();

        var id = Guid.NewGuid();
        var satellite = composition.Initialize(new Satellite(), id);
        satellite.Id.ShouldBe(id);
        satellite.Telemetry.ShouldBeOfType<TelemetrySystem>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface ITelemetrySystem;

class TelemetrySystem : ITelemetrySystem;

interface ISatellite
{
    Guid Id { get; }

    ITelemetrySystem? Telemetry { get; }
}

record Satellite : ISatellite
{
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public ITelemetrySystem? Telemetry { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
}
// }