/*
$v=true
$p=15
$d=Method injection
$sa=Property injection
$sa=Field injection
$sa=Dependency attribute
$h=To use dependency injection for a method, simply add the _Dependency_ (or _Ordinal_) attribute to that method, specifying the sequence number that will be used to define the call to that method:
$f=Method injection is how a type receives dependencies a constructor cannot take: initialization that must run after construction, or a value the type deliberately does not hold as state.
$f=The method must be non-static and callable from the generated composition, and its parameters are resolved like constructor parameters, tags included; the ordinal fixes its place among other injected members.
$f=Only methods marked with `Dependency` (or `Ordinal`) are called. The call happens inside the generated construction sequence, so a method that throws breaks resolution of the whole root.
$f=Prefer constructor injection for required dependencies and reserve method injection for late or optional initialization, such as wiring an event handler.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.MethodInjectionScenario;

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
            .Bind<IMap>().To<Map>()
            .Bind<INavigator>().To<Navigator>()

            // Composition root
            .Root<INavigator>("Navigator");

        var composition = new Composition();
        var navigator = composition.Navigator;
        navigator.CurrentMap.ShouldBeOfType<Map>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IMap;

class Map : IMap;

interface INavigator
{
    IMap? CurrentMap { get; }
}

class Navigator : INavigator
{
    // The Dependency (or Ordinal) attribute indicates that the method
    // should be called to inject the dependency.
    [Dependency(ordinal: 0)]
    public void LoadMap(IMap map) =>
        CurrentMap = map;

    public IMap? CurrentMap { get; private set; }
}
// }