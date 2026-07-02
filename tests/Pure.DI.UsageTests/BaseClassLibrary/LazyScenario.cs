/*
$v=true
$p=8
$d=Lazy
$sa=Func
$sa=Manually started tasks
$h=Injecting `Lazy<T>` defers creation of a dependency until its `Value` property is first accessed, after which the same instance is returned every time. No extra setup is needed: bind the underlying type as usual and request `Lazy<T>` in the constructor.
$f=>[!NOTE]
$f=>Lazy<T> is useful for expensive-to-create objects or when the instance may never be needed, improving application startup performance.
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
        // Disable Resolve methods to keep the public API minimal
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