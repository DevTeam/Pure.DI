/*
$v=true
$p=3
$d=Injections on demand with arguments
$h=This example illustrates dependency injection with parameterized factory functions using Pure.DI, where dependencies are created with runtime-provided arguments. The scenario features a service that generates dependencies with specific IDs passed during instantiation.
$f=Delayed dependency instantiation:
$f=- Injection of dependencies requiring runtime parameters
$f=- Creation of distinct instances with different configurations
$f=- Type-safe resolution of dependencies with constructor arguments
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.InjectionOnDemandWithArgumentsScenario;

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
            .Bind().To<Sensor>()
            .Bind().To<SmartHome>()

            // Composition root
            .Root<ISmartHome>("SmartHome");

        var composition = new Composition();
        var smartHome = composition.SmartHome;
        var sensors = smartHome.Sensors;

        sensors.Count.ShouldBe(2);
        sensors[0].Id.ShouldBe(101);
        sensors[1].Id.ShouldBe(102);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface ISensor
{
    int Id { get; }
}

class Sensor(int id) : ISensor
{
    public int Id { get; } = id;
}

interface ISmartHome
{
    IReadOnlyList<ISensor> Sensors { get; }
}

class SmartHome(Func<int, ISensor> sensorFactory) : ISmartHome
{
    public IReadOnlyList<ISensor> Sensors { get; } =
    [
        // Use the injected factory to create a sensor with ID 101 (e.g., Kitchen Temperature)
        sensorFactory(101),

        // Create another sensor with ID 102 (e.g., Living Room Humidity)
        sensorFactory(102)
    ];
}
// }