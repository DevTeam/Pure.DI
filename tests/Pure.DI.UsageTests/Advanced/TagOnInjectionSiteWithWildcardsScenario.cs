/*
$v=true
$p=6
$d=Tag on injection site with wildcards
$h=The wildcards ‘*’ and ‘?’ are supported.
$f=> [!WARNING]
$f=> Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedTypeParameter

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.TagOnInjectionSiteWithWildcardsScenario;

using Pure.DI;
using UsageTests;
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
            // We use wildcards to specify logic:
            // 1. Inject TemperatureSensor into 'OutdoorSensor' property of SmartHomeSystem
            // 2. Inject TemperatureSensor into 'sensor' argument of any ClimateControl
            .Bind(Tag.On("*SmartHomeSystem:OutdoorSensor", "*ClimateControl:sensor"))
            .To<TemperatureSensor>()

            // Inject MotionSensor into any argument starting with 'zone' inside SmartHomeSystem
            // This corresponds to 'zone1' and 'zone2'
            .Bind(Tag.On("*SmartHomeSystem:zone?"))
            .To<MotionSensor>()
            .Bind<ISmartHomeSystem>().To<SmartHomeSystem>()

            // Specifies to create the composition root named "Root"
            .Root<ISmartHomeSystem>("SmartHome");

        var composition = new Composition();
        var smartHome = composition.SmartHome;

        // Verification:
        // Zone sensors should be MotionSensors (matched by "*SmartHomeSystem:zone?")
        smartHome.Zone1.ShouldBeOfType<MotionSensor>();
        smartHome.Zone2.ShouldBeOfType<MotionSensor>();

        // Outdoor sensor should be TemperatureSensor (matched by "*SmartHomeSystem:OutdoorSensor")
        smartHome.OutdoorSensor.ShouldBeOfType<TemperatureSensor>();

        // Climate control sensor should be TemperatureSensor (matched by "*ClimateControl:sensor")
        smartHome.ClimateSensor.ShouldBeOfType<TemperatureSensor>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface ISensor;

class TemperatureSensor : ISensor;

class MotionSensor : ISensor;

class ClimateControl<T>(ISensor sensor)
{
    public ISensor Sensor { get; } = sensor;
}

interface ISmartHomeSystem
{
    ISensor Zone1 { get; }

    ISensor Zone2 { get; }

    ISensor OutdoorSensor { get; }

    ISensor ClimateSensor { get; }
}

class SmartHomeSystem(
    ISensor zone1,
    ISensor zone2,
    ClimateControl<string> climateControl)
    : ISmartHomeSystem
{
    public ISensor Zone1 { get; } = zone1;

    public ISensor Zone2 { get; } = zone2;

    public required ISensor OutdoorSensor { init; get; }

    public ISensor ClimateSensor => climateControl.Sensor;
}
// }