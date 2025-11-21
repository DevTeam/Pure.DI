/*
$v=true
$p=3
$d=Tag attribute
$h=Sometimes it's important to take control of building a dependency graph. For example, when there are multiple implementations of the same contract. In this case, _tags_ will help:
$f=The tag can be a constant, a type, a [smart tag](smart-tags.md), or a value of an `Enum` type. This attribute is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.TagAttributeScenario;

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
            .Bind("Fast").To<FastRenderer>()
            .Bind("Quality").To<QualityRenderer>()
            .Bind().To<PageRenderer>()

            // Composition root
            .Root<IPageRenderer>("Renderer");

        var composition = new Composition();
        var pageRenderer = composition.Renderer;
        pageRenderer.FastRenderer.ShouldBeOfType<FastRenderer>();
        pageRenderer.QualityRenderer.ShouldBeOfType<QualityRenderer>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IRenderer;

class FastRenderer : IRenderer;

class QualityRenderer : IRenderer;

interface IPageRenderer
{
    IRenderer FastRenderer { get; }

    IRenderer QualityRenderer { get; }
}

class PageRenderer(
    [Tag("Fast")] IRenderer fastRenderer,
    [Tag("Quality")] IRenderer qualityRenderer)
    : IPageRenderer
{
    public IRenderer FastRenderer { get; } = fastRenderer;

    public IRenderer QualityRenderer { get; } = qualityRenderer;
}
// }