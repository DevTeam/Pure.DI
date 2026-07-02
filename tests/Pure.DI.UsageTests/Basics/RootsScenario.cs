/*
$v=true
$p=30
$d=Roots
$sa=Roots with filter
$sa=Generic roots
$h=Sometimes you need roots for all types inherited from <see cref="T"/> available at compile time at the point where the method is called.
$f=>[!NOTE]
$f=>This feature is useful for plugin-style architectures where you need to expose all implementations of a base type or interface.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.RootsScenario;

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
            .Bind().As(Lifetime.Singleton).To<Preferences>()
            // Roots can be used to register all descendants of a type as roots.
            .Roots<IWindow>("{type}");

        var composition = new Composition();
        composition.MainWindow.ShouldBeOfType<MainWindow>();
        composition.SettingsWindow.ShouldBeOfType<SettingsWindow>();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IPreferences;

class Preferences : IPreferences;

interface IWindow;

class MainWindow(IPreferences preferences) : IWindow;

class SettingsWindow(IPreferences preferences) : IWindow;
// }