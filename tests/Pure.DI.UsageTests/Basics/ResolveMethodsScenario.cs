/*
$v=true
$p=1
$d=Resolve methods
$h=This example shows how to resolve dependencies via generated `Resolve` methods, i.e. through the _Service Locator_ style.
$h=Use this style mainly for integration scenarios; explicit roots are usually cleaner and safer.
$f=_Resolve_ methods are similar to calling composition roots, which are properties (or methods). Roots are efficient and do not throw, so they are preferred. In contrast, _Resolve_ methods have drawbacks:
$f=- They provide access to an unlimited set of dependencies (_Service Locator_).
$f=- Their use can potentially lead to runtime exceptions. For example, when the corresponding root has not been defined.
$f=- They are awkward for some UI binding scenarios (e.g., MAUI/WPF/Avalonia).
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
        var humiditySensor3 = composition.HumiditySensor; // Resolve via the public root
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
