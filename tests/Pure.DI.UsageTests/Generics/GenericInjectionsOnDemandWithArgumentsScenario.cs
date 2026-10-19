/*
$v=true
$p=9
$d=Generic injections on demand with arguments
$sa=Generic injections on demand
$sa=Injections on demand with arguments
$h=When creating a generic dependency requires a runtime value, inject a factory with arguments. `SensorHub<T>` receives a `Func<int, ISensor<T>>` and calls it with a specific `id` for each sensor; the `int` argument is mapped to the `Sensor<T>` constructor parameter.
$h=This keeps the composition in charge of wiring while letting the consumer supply per-instance data at creation time.
$f=>[!NOTE]
$f=>Generic factories with arguments allow passing runtime parameters while maintaining type safety.
$f=
$f=This is how a sensor registry, a document loader or a connection pool hands out per-item instances while the item identifiers come from the caller. The consumer owns the list, the composition owns everything the items depend on.
$f=Each call resolves the graph from scratch, so an instance created this way is not shared with earlier ones even if the type would otherwise be a singleton in other bindings. Keep the factory's arguments stable - they are matched to constructor parameters by type and tag.
$f=>[!NOTE]
$f=>Arguments of the same type on one constructor compete for a single factory argument. Tag the parameters when two runtime values share a type.
$f=
$f=Prefer a plain `Func<T>` factory when no per-instance data is needed (see [Generic injections on demand](generic-injections-on-demand.md)), and root arguments when the value is needed once per root call.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedTypeParameter
namespace Pure.DI.UsageTests.Generics.GenericInjectionsOnDemandWithArgumentsScenario;

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
            .Bind().To<Sensor<TT>>()
            .Bind().To<SensorHub<TT>>()

            // Composition root
            .Root<ISensorHub<string>>("SensorHub");

        var composition = new Composition();
        var hub = composition.SensorHub;
        var sensors = hub.Sensors;
        sensors.Count.ShouldBe(2);
        sensors[0].Id.ShouldBe(1);
        sensors[1].Id.ShouldBe(2);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface ISensor<out T>
{
    int Id { get; }
}

class Sensor<T>(int id) : ISensor<T>
{
    public int Id { get; } = id;
}

interface ISensorHub<out T>
{
    IReadOnlyList<ISensor<T>> Sensors { get; }
}

class SensorHub<T>(Func<int, ISensor<T>> sensorFactory) : ISensorHub<T>
{
    public IReadOnlyList<ISensor<T>> Sensors { get; } =
    [
        sensorFactory(1),
        sensorFactory(2)
    ];
}
// }