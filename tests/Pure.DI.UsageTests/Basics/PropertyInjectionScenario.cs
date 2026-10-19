/*
$v=true
$p=16
$d=Property injection
$sa=Method injection
$sa=Field injection
$sa=Required properties or fields
$h=To use dependency injection on a property, make sure the property is writable and simply add the _Ordinal_ attribute to that property, specifying the ordinal that will be used to determine the injection order:
$h=Property injection is usually for optional dependencies: the type works without the property, and the composition fills it when a binding exists.
$f=The property must have a setter and must not be static; an `init` setter is enough only while the composition creates the object itself, not when build-up runs later.
$f=The `Dependency` (or `Ordinal`) attribute is used to mark the property for injection, and the integer fixes the order among other injected members.
$f=A nullable property (`ILogger?`) documents that `null` is acceptable; a non-nullable one is resolved or the graph fails, so the nullability annotation decides what happens when no binding exists.
$f=Prefer constructor injection for required dependencies, and a `required` property when the type must be immutable after construction.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.PropertyInjectionScenario;

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
            .Bind<ILogger>().To<ConsoleLogger>()
            .Bind<IService>().To<Service>()

            // Composition root
            .Root<IService>("MyService");

        var composition = new Composition();
        var service = composition.MyService;
        service.Logger.ShouldBeOfType<ConsoleLogger>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface ILogger;

class ConsoleLogger : ILogger;

interface IService
{
    ILogger? Logger { get; }
}

class Service : IService
{
    // The Dependency attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection.
    // Usually, property injection is used for optional dependencies.
    [Dependency] public ILogger? Logger { get; set; }
}
// }