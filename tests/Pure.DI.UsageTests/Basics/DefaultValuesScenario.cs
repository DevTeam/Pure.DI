/*
$v=true
$p=18
$d=Default values
$sa=Required properties or fields
$h=When an injection cannot be satisfied, Pure.DI falls back to the C# default the consumer declared: a constructor parameter default such as `systemName = "Home Guard"`, or a property initializer behind `required`.
$h=This is how an optional dependency and a sensible default value coexist without a separate configuration flag.
$f=The fallback applies only where the consumer declared a default; when no binding and no default exist, the graph fails at compile time (`DIE000`), so a default is not a way to ignore a missing binding.
$f=An explicit binding always wins over the declared default, which makes the class usable both as a self-contained entity and as a composed service.
$f=A `required` property with an initializer is assigned by the generated code when a binding exists; the initializer is what the language requires for construction without the member.
$f=Prefer an explicit nullable dependency when absence is a real state, and defaults for configuration-like values such as a name or a timeout.
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
        // Disable Resolve methods to keep the public API minimal
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