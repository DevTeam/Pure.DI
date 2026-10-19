/*
$v=true
$p=25
$d=Overrides
$sa=Composition arguments
$sa=Root arguments
$sa=Func with arguments
$sa=Thread-safe overrides
$h=This example shows how `ctx.Override(...)` customizes the values a constructor receives per call: a factory `Func<int, int, IWidget>` overrides the untagged `id`, the tagged `layer` and the `name`, then injects the widget and returns it.
$h=An override is how a value that changes per invocation - a request id, a tenant, a computed label - reaches a type whose constructor also needs normal dependencies, without turning every parameter into a root argument or a service.
$f=Overrides are scoped to the factory invocation that declares them; nothing is captured by the composition, so a singleton cannot be built from overridden values in a meaningful way.
$f=An override is matched by value type and tag, so a tagged override (`ctx.Override(layerIndex, "layer")`) is required when two parameters share a type; an override that matches nothing is silently unused.
$f=The factory itself is resolved once per `Func` use and then invoked per call, so keep the overrides close to the injection they affect for readability.
$f=`FormatCode = On` is set here only to keep the generated code readable; it costs generation time and belongs to debugging, not to a release composition.
$f=Prefer a root argument when the value is the same for a whole root, and `Override` when one factory call produces many differently configured instances.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable VariableHidesOuterVariable

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.OverridesScenario;

using System.Collections.Immutable;
using System.Drawing;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Immutable;
//# using System.Drawing;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
        // FormatCode = On
// {    
        DI.Setup(nameof(Composition))
            .Bind(Tag.Red).To(() => Color.Red)
            .Bind().As(Lifetime.Singleton).To<Clock>()
            // The factory accepts the widget ID and the layer index
            .Bind().To<Func<int, int, IWidget>>(ctx =>
                (widgetId, layerIndex) => {
                    // Overrides the 'id' argument of the constructor with the first lambda argument
                    ctx.Override(widgetId);

                    // Overrides the 'layer' tagged argument of the constructor with the second lambda argument
                    ctx.Override(layerIndex, "layer");

                    // Overrides the 'name' argument with a formatted string
                    ctx.Override($"Widget {widgetId} on layer {layerIndex}");

                    // Resolves the 'Color' dependency tagged with 'Red'
                    ctx.Inject(Tag.Red, out Color color);
                    // Overrides the 'color' argument with the resolved value
                    ctx.Override(color);

                    // Creates the instance using the overridden values
                    ctx.Inject<Widget>(out var widget);
                    return widget;
                })
            .Bind().To<Dashboard>()

            // Composition root
            .Root<IDashboard>("Dashboard");

        var composition = new Composition();
        var dashboard = composition.Dashboard;
        dashboard.Widgets.Length.ShouldBe(3);

        dashboard.Widgets[0].Id.ShouldBe(0);
        dashboard.Widgets[0].Layer.ShouldBe(99);
        dashboard.Widgets[0].Name.ShouldBe("Widget 0 on layer 99");

        dashboard.Widgets[1].Id.ShouldBe(1);
        dashboard.Widgets[1].Name.ShouldBe("Widget 1 on layer 99");

        dashboard.Widgets[2].Id.ShouldBe(2);
        dashboard.Widgets[2].Name.ShouldBe("Widget 2 on layer 99");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IClock
{
    DateTimeOffset Now { get; }
}

class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IWidget
{
    string Name { get; }

    int Id { get; }

    int Layer { get; }
}

class Widget(
    string name,
    IClock clock,
    int id,
    [Tag("layer")] int layer,
    Color color)
    : IWidget
{
    public string Name => name;

    public int Id => id;

    public int Layer => layer;
}

interface IDashboard
{
    ImmutableArray<IWidget> Widgets { get; }
}

class Dashboard(Func<int, int, IWidget> widgetFactory) : IDashboard
{
    public ImmutableArray<IWidget> Widgets { get; } =
    [
        widgetFactory(0, 99),
        widgetFactory(1, 99),
        widgetFactory(2, 99)
    ];
}
// }