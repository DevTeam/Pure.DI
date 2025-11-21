/*
$v=true
$p=15
$d=Default values
$h=This example demonstrates how to use default values in dependency injection when explicit injection is not possible.
$f=The key points are:
$f=- Default constructor arguments can be used for simple values
$f=- The DI container will use these defaults if no explicit bindings are provided
$f=
$f=This example illustrates how to handle default values in a dependency injection scenario:
$f=- **Constructor Default Argument**: The `SecuritySystem` class has a constructor with a default value for the name parameter. If no value is provided, "Home Guard" will be used.
$f=- **Required Property with Default**: The `Sensor` property is marked as required but has a default instantiation. This ensures that:
$f=  - The property must be set
$f=  - If no explicit injection occurs, a default value will be used
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.DefaultValuesScenario;

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
            .Bind<ISensor>().To<MotionSensor>()
            .Bind<ISecuritySystem>().To<SecuritySystem>()

            // Composition root
            .Root<ISecuritySystem>("SecuritySystem");

        var composition = new Composition();
        var securitySystem = composition.SecuritySystem;
        securitySystem.Sensor.ShouldBeOfType<MotionSensor>();
        securitySystem.SystemName.ShouldBe("Home Guard");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface ISensor;

class MotionSensor : ISensor;

interface ISecuritySystem
{
    string SystemName { get; }

    ISensor Sensor { get; }
}

// If injection cannot be performed explicitly,
// the default value will be used
class SecuritySystem(string systemName = "Home Guard") : ISecuritySystem
{
    public string SystemName { get; } = systemName;

    // The 'required' modifier ensures that the property is set during initialization.
    // The default value 'new MotionSensor()' provides a fallback
    // if no dependency is injected.
    public required ISensor Sensor { get; init; } = new MotionSensor();
}
// }