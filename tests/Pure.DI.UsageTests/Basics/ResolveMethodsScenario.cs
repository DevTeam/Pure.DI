/*
$v=true
$p=1
$d=Resolve methods
$h=This example shows how to resolve the roots of a composition using `Resolve` methods to use the composition as a _Service Locator_. The `Resolve` methods are generated automatically without additional effort.
$f=_Resolve_ methods are similar to calls to composition roots. Composition roots are properties (or methods). Their use is efficient and does not cause exceptions. This is why they are recommended to be used. In contrast, _Resolve_ methods have a number of disadvantages:
$f=- They provide access to an unlimited set of dependencies (_Service Locator_).
$f=- Their use can potentially lead to runtime exceptions. For example, when the corresponding root has not been defined.
$f=- Sometimes cannot be used directly, e.g., for MAUI/WPF/Avalonia binding.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.ResolveMethodsScenario;

using System.Diagnostics.CodeAnalysis;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

// ... existing code ...
[SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known")]
public class Scenario
{
    [Fact]
    public void Run()
    {
        // {
        DI.Setup(nameof(Composition))
            .Bind<IDevice>().To<Device>()
            .Bind<ISensor>().To<TemperatureSensor>()
            .Bind<ISensor>("Humidity").To<HumiditySensor>()

            // Specifies to create a private root
            // that is only accessible from _Resolve_ methods
            .Root<ISensor>()

            // Specifies to create a public root named _HumiditySensor_
            // using the "Humidity" tag
            .Root<ISensor>("HumiditySensor", "Humidity");

        var composition = new Composition();

        // The next 3 lines of code do the same thing:
        var sensor1 = composition.Resolve<ISensor>();
        var sensor2 = composition.Resolve(typeof(ISensor));
        var sensor3 = composition.Resolve(typeof(ISensor), null);

        // Resolve by "Humidity" tag
        // The next 3 lines of code do the same thing too:
        var humiditySensor1 = composition.Resolve<ISensor>("Humidity");
        var humiditySensor2 = composition.Resolve(typeof(ISensor), "Humidity");
        var humiditySensor3 = composition.HumiditySensor; // Gets the composition through the public root
        // }
        sensor1.ShouldBeOfType<TemperatureSensor>();
        sensor2.ShouldBeOfType<TemperatureSensor>();
        sensor3.ShouldBeOfType<TemperatureSensor>();
        humiditySensor1.ShouldBeOfType<HumiditySensor>();
        humiditySensor2.ShouldBeOfType<HumiditySensor>();
        humiditySensor3.ShouldBeOfType<HumiditySensor>();
        composition.SaveClassDiagram();
    }
}

// {
interface IDevice;

class Device : IDevice;

interface ISensor;

class TemperatureSensor(IDevice device) : ISensor;

class HumiditySensor : ISensor;
// }