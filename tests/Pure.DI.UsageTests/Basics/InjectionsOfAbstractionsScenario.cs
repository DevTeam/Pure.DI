/*
$v=true
$p=0
$d=Injections of abstractions
$h=This example demonstrates the recommended approach of using abstractions instead of implementations when injecting dependencies.
$f=Usually the biggest block in the setup is the chain of bindings, which describes which implementation corresponds to which abstraction. This is necessary so that the code generator can build a composition of objects using only NOT abstract types. This is true because the cornerstone of DI technology implementation is the principle of abstraction-based programming rather than concrete class-based programming. Thanks to it, it is possible to replace one concrete implementation by another. And each implementation can correspond to an arbitrary number of abstractions.
$f=> [!TIP]
$f=> Even if the binding is not defined, there is no problem with the injection, but obviously under the condition that the consumer requests an injection NOT of abstract type.
$f=
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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // Binding abstractions to their implementations:
            // The interface IGpsSensor is bound to the implementation GpsSensor
            .Bind<IGpsSensor>().To<GpsSensor>()
            // The interface INavigationSystem is bound to the implementation NavigationSystem
            .Bind<INavigationSystem>().To<NavigationSystem>()

            // Specifies to create a composition root
            // of type "VehicleComputer" with the name "Root"
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