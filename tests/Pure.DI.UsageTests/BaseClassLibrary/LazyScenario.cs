/*
$v=true
$p=3
$d=Lazy
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.BCL.LazyScenario;

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
            .Bind<IGraphicsEngine>().To<GraphicsEngine>()
            .Bind<IWindow>().To<Window>()

            // Composition root
            .Root<IWindow>("Window");

        var composition = new Composition();
        var window = composition.Window;

        // The graphics engine is created only when it is first accessed
        window.Engine.ShouldBe(window.Engine);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IGraphicsEngine;

class GraphicsEngine : IGraphicsEngine;

interface IWindow
{
    IGraphicsEngine Engine { get; }
}

class Window(Lazy<IGraphicsEngine> engine) : IWindow
{
    public IGraphicsEngine Engine => engine.Value;
}
// }