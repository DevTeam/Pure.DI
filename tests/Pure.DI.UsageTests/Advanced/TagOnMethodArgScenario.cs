/*
$v=true
$p=6
$d=Tag on a method argument
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
namespace Pure.DI.UsageTests.Advanced.TagOnMethodArgScenario;

using System.Diagnostics.CodeAnalysis;
using Pure.DI;
using UsageTests;
using Xunit;

// {
//# using Pure.DI;
// }

[SuppressMessage("WRN", "DIW001:WRN")]
public class Scenario
{
    [Fact]
    public void Run()
    {
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind().To<TemperatureSensor>()
            // Binds specifically to the argument "sensor" of the "Calibrate" method
            // in the "WeatherStation" class
            .Bind(Tag.OnMethodArg<WeatherStation>(nameof(WeatherStation.Calibrate), "sensor"))
            .To<HumiditySensor>()
            .Bind<IWeatherStation>().To<WeatherStation>()

            // Specifies to create the composition root named "Station"
            .Root<IWeatherStation>("Station");

        var composition = new Composition();
        var station = composition.Station;
        station.Sensor.ShouldBeOfType<HumiditySensor>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface ISensor;

class TemperatureSensor : ISensor;

class HumiditySensor : ISensor;

interface IWeatherStation
{
    ISensor? Sensor { get; }
}

class WeatherStation : IWeatherStation
{
    // The [Dependency] attribute is used to mark the method for injection
    [Dependency]
    public void Calibrate(ISensor sensor) =>
        Sensor = sensor;

    public ISensor? Sensor { get; private set; }
}
// }