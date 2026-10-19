/*
$v=true
$t=QuickStart
$t=Basics
$p=1
$p=QuickStart:1
$d=Injections of abstractions
$sa=Auto-bindings
$sa=Composition roots
$h=This is the recommended model for production code: depend on abstractions and bind them to implementations in composition.
$h=It keeps business code independent from infrastructure details, makes replacements predictable, and gives one place where a lifetime or a decorator is changed for the whole application.
$f=The binding chain maps abstractions to concrete types so the generator can build a fully concrete object graph; a single implementation can satisfy several abstractions at once.
$f=The compiler enforces the contract: binding an implementation that does not implement the abstraction is `DIE011`, and an abstraction that nothing satisfies is a graph error, not a runtime surprise.
$f=>[!TIP]
$f=>A consumer that requests a concrete type still resolves without a binding, so a leak of a concrete type into business code stays invisible - the abstractions above are what make the graph explicit.
$f=
$f=Prefer this style over [auto-bindings](auto-bindings.md); add tags only when the same abstraction really has several implementations.
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
