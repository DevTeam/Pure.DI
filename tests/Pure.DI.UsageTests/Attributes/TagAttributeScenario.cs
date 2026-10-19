/*
$v=true
$p=3
$d=Tag attribute
$sa=Tags
$sa=Smart tags
$h=Tags let you choose among multiple implementations of the same contract. `[Tag(...)]` places that choice at the injection site, so the consuming type states which implementation it needs while the composition still owns the bindings. Use it when one class legitimately depends on two implementations of the same interface, as `PageRenderer` does with a fast and a quality renderer.
$f=This is the usual way to keep an implementation choice next to the consumer when the consumer's code is yours: the setup binds each implementation once with its tag, and every injection site picks one of them. A codebase with many tags usually replaces string literals with an enum or a [smart tag](smart-tags.md), so a mistyped tag cannot compile.
$f=A tag that matches no binding leaves the contract unresolved, and Pure.DI reports that at compile time instead of failing at run time. The tag is part of the injection-site contract, so renaming one touches the setup and every consumer in the same change.
$f=A tag can be a constant, a type, a [smart tag](smart-tags.md), or an enum value. The `Tag` attribute is part of the API, but you can define your own in any assembly or namespace.
$f=>[!TIP]
$f=>Prefer enum values or smart tags once a graph has more than a couple of string tags, and reserve `[Tag("...")]` for small graphs where the literal is obvious.
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
        // Disable Resolve methods to keep the public API minimal
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
