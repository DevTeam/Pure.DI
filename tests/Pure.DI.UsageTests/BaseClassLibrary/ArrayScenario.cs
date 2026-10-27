/*
$v=true
$p=5
$d=Array
$sa=Enumerable
$sa=Span and ReadOnlySpan
$h=Specifying `T[]` as the injection type allows instances from all bindings that implement the `T` type to be injected. It is the way to get "every handler of this contract" without registering a composite by hand.
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
$f=The array contains one element for each distinct tag of `ISensor`, including no tag, in the order the bindings are declared; here the untagged `TemperatureSensor` comes before the tagged `WindSensor`. When several bindings share a tag, or none of them has a tag, the last declared one overrides the others and only it is included; use `Tag.Unique` to keep every one of them. An array is created eagerly, so all elements exist as soon as the consumer is created.
$f=Adding a binding for the same contract changes the length of every injected array of that contract, which is why a test that asserts the count or the order is worth keeping. A consumer that must not see a particular implementation needs a separate contract or a tag on the collection parameter. A binding never receives itself: a composite that implements `ISensor` and injects `ISensor[]` gets the other bindings of `ISensor`.
$f=>[!TIP]
$f=>Prefer `IEnumerable<T>` when the consumer may not need every element, and an array or `ImmutableArray<T>` when it reads them repeatedly.
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
        // Disable Resolve methods to keep the public API minimal
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