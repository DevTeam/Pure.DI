/*
$v=true
$p=14
$d=Generic injections on demand with arguments
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedTypeParameter
namespace Pure.DI.UsageTests.Generics.GenericInjectionsAsRequiredWithArgumentsScenario;

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