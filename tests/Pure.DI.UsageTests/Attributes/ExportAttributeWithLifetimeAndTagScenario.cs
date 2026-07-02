/*
$v=true
$p=14
$d=Export attribute with lifetime and tag
$h=The `[Export]` attribute accepts optional `lifetime` and `tags` parameters, so an exported member is registered exactly like a hand-written binding. Here the `GraphicsAdapter.HighPerfGpu` property is exported as a `Singleton` with the tag `"HighPerformance"`, and `RayTracer` receives that instance by requesting `[Tag("HighPerformance")] IGpu`.
$f=>[!NOTE]
$f=>Specifying lifetime and tag in the Export attribute allows for fine-grained control over instance creation and binding resolution.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable LocalizableElement
namespace Pure.DI.UsageTests.Basics.ExportAttributeWithLifetimeAndTagScenario;

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
    // Binds the property to the composition with the specified
    // lifetime and tag. This allows the "HighPerformance" GPU
    // to be injected into other components.
    [Export(lifetime: Lifetime.Singleton, tags: ["HighPerformance"])]
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