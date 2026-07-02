/*
$v=true
$p=14
$d=Generic injections on demand with arguments
$h=When creating a generic dependency requires a runtime value, inject a factory with arguments. `SensorHub<T>` receives a `Func<int, ISensor<T>>` and calls it with a specific `id` for each sensor; the `int` argument is mapped to the `Sensor<T>` constructor parameter.
$h=This keeps the composition in charge of wiring while letting the consumer supply per-instance data at creation time.
$f=>[!NOTE]
$f=>Generic factories with arguments allow passing runtime parameters while maintaining type safety.
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