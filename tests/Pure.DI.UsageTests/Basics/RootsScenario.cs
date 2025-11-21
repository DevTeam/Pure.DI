/*
$v=true
$p=20
$d=Roots
$h=Sometimes you need roots for all types inherited from <see cref="T"/> available at compile time at the point where the method is called.
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
        // This hint indicates to not generate methods such as Resolve
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