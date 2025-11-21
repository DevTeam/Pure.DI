/*
$v=true
$p=14
$d=Bind attribute with lifetime and tag
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable LocalizableElement
namespace Pure.DI.UsageTests.Basics.BindAttributeWithLifetimeAndTagScenario;

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
            .Bind().As(Lifetime.Singleton).To<GraphicsAdapter>()
            .Bind().To<RayTracer>()

            // Composition root
            .Root<IRenderer>("Renderer");

        var composition = new Composition();
        var renderer = composition.Renderer;
        renderer.Render();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IGpu
{
    void RenderFrame();
}

class DiscreteGpu : IGpu
{
    public void RenderFrame() => Console.WriteLine("Rendering with Discrete GPU");
}

class GraphicsAdapter
{
    // Binds the property to the container with the specified
    // lifetime and tag. This allows the "HighPerformance" GPU
    // to be injected into other components.
    [Bind(lifetime: Lifetime.Singleton, tags: ["HighPerformance"])]
    public IGpu HighPerfGpu { get; } = new DiscreteGpu();
}

interface IRenderer
{
    void Render();
}

class RayTracer([Tag("HighPerformance")] IGpu gpu) : IRenderer
{
    public void Render() => gpu.RenderFrame();
}
// }