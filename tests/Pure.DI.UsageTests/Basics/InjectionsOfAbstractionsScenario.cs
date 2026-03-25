/*
$v=true
$p=0
$d=Injections of abstractions
$h=This is the recommended model for production code: depend on abstractions and bind them to implementations in composition.
$h=It keeps business code independent from infrastructure details and makes replacements predictable.
$f=The binding chain maps abstractions to concrete types so the generator can build a fully concrete object graph. This keeps consumers decoupled and allows swapping implementations. A single implementation can satisfy multiple abstractions.
$f=>[!TIP]
$f=>If a binding is missing, injection still works when the consumer requests a concrete type (not an abstraction).
$f=
$f=Limitations: explicit bindings add configuration lines, but the trade-off is clearer architecture and safer evolution.
$f=Common pitfalls:
$f=- Mixing abstraction-first and concrete-only styles in one module without clear boundaries.
$f=- Forgetting to bind alternate implementations for tagged use cases.
$f=See also: [Auto-bindings](auto-bindings.md), [Tags](tags.md).
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.InjectionsOfAbstractionsScenario;

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
            // Binding abstractions to their implementations:
            // The interface IGpsSensor is bound to the implementation GpsSensor
            .Bind<IGpsSensor>().To<GpsSensor>()
            // The interface INavigationSystem is bound to the implementation NavigationSystem
            .Bind<INavigationSystem>().To<NavigationSystem>()

            // Specifies to create a composition root
            // of type "VehicleComputer" with the name "VehicleComputer"
            .Root<VehicleComputer>("VehicleComputer");

        var composition = new Composition();

        // Usage:
        // var vehicleComputer = new VehicleComputer(new NavigationSystem(new GpsSensor()));
        var vehicleComputer = composition.VehicleComputer;

        vehicleComputer.StartTrip();
// }
        composition.SaveClassDiagram();
    }
}

// {
// The sensor abstraction
interface IGpsSensor;

// The sensor implementation
class GpsSensor : IGpsSensor;

// The service abstraction
interface INavigationSystem
{
    void Navigate();
}

// The service implementation
class NavigationSystem(IGpsSensor sensor) : INavigationSystem
{
    public void Navigate()
    {
        // Navigation logic using the sensor...
    }
}

// The consumer of the abstraction
partial class VehicleComputer(INavigationSystem navigationSystem)
{
    public void StartTrip() => navigationSystem.Navigate();
}
// }
