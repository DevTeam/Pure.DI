/*
$v=true
$p=15
$d=Method injection
$sa=Property injection
$sa=Field injection
$sa=Dependency attribute
$h=To use dependency injection for a method, simply add the _Dependency_ (or _Ordinal_) attribute to that method, specifying the sequence number that will be used to define the call to that method:
$f=The key points are:
$f=- The method must be available to be called from a composition class
$f=- The `Dependency` (or `Ordinal`) attribute is used to mark the method for injection
$f=- The DI automatically calls the method to inject dependencies
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