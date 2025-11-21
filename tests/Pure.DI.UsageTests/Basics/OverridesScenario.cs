/*
$v=true
$p=16
$d=Overrides
$h=This example demonstrates advanced dependency injection techniques using Pure.DI's override mechanism to customize dependency instantiation with runtime arguments and tagged parameters. The implementation creates multiple `IDependency` instances with values manipulated through explicit overrides.
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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
        // FormatCode = On
// {    
        DI.Setup(nameof(Composition))
            .Bind(Tag.Red).To(_ => Color.Red)
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