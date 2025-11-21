/*
$v=true
$p=2
$d=Array
$h=Specifying `T[]` as the injection type allows instances from all bindings that implement the `T` type to be injected.
$f=In addition to arrays, other collection types are also supported, such as:
$f=- System.Memory<T>
$f=- System.ReadOnlyMemory<T>
$f=- System.Span<T>
$f=- System.ReadOnlySpan<T>
$f=- System.Collections.Generic.ICollection<T>
$f=- System.Collections.Generic.IList<T>
$f=- System.Collections.Generic.List<T>
$f=- System.Collections.Generic.IReadOnlyCollection<T>
$f=- System.Collections.Generic.IReadOnlyList<T>
$f=- System.Collections.Generic.ISet<T>
$f=- System.Collections.Generic.HashSet<T>
$f=- System.Collections.Generic.SortedSet<T>
$f=- System.Collections.Generic.Queue<T>
$f=- System.Collections.Generic.Stack<T>
$f=- System.Collections.Immutable.ImmutableArray<T>
$f=- System.Collections.Immutable.IImmutableList<T>
$f=- System.Collections.Immutable.ImmutableList<T>
$f=- System.Collections.Immutable.IImmutableSet<T>
$f=- System.Collections.Immutable.ImmutableHashSet<T>
$f=- System.Collections.Immutable.ImmutableSortedSet<T>
$f=- System.Collections.Immutable.IImmutableQueue<T>
$f=- System.Collections.Immutable.ImmutableQueue<T>
$f=- System.Collections.Immutable.IImmutableStack<T>
$f=And of course this list can easily be supplemented on its own.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.BCL.ArrayScenario;

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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind<ISensor>().To<TemperatureSensor>()
            .Bind<ISensor>("External").To<WindSensor>()
            .Bind<ISensorService>().To<SensorService>()

            // Composition root
            .Root<ISensorService>("Sensor");

        var composition = new Composition();
        var sensor = composition.Sensor;

        // Checks that all bindings for the ISensor interface are injected,
        // regardless of whether they are tagged or not.
        sensor.Sensors.Length.ShouldBe(2);
        sensor.Sensors[0].ShouldBeOfType<TemperatureSensor>();
        sensor.Sensors[1].ShouldBeOfType<WindSensor>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface ISensor;

class TemperatureSensor : ISensor;

class WindSensor : ISensor;

interface ISensorService
{
    ISensor[] Sensors { get; }
}

class SensorService(ISensor[] sensors) : ISensorService
{
    public ISensor[] Sensors { get; } = sensors;
}
// }