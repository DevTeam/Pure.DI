/*
$v=true
$p=9
$d=Generate interfaces with generics
$h=This example shows how generic members, nullable annotations, and events are preserved in a reporting scenario.
$f=The example shows how to:
$f=- Generate an interface for generic members
$f=- Preserve nullable annotations
$f=- Preserve events and generic constraints
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable CS0067
namespace Pure.DI.UsageTests.Interfaces.GenerateInterfaceGenericsScenario;

using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // {
        DI.Setup(nameof(Composition))
            .Bind().To<ReportFormatter>()
            .Root<App>(nameof(App));

        var composition = new Composition();
        var app = composition.App;

        app.Formatted.ShouldBe("Order #42");
        app.Title.ShouldBe("Daily Report");
        // }

        composition.SaveClassDiagram();
    }
}

// {
public partial interface IReportFormatter;

[GenerateInterface]
public class ReportFormatter : IReportFormatter
{
    public string? Title { get; set; } = "Daily Report";

    public event EventHandler? Changed;

    public string? Format<T>(T value)
        where T : class
        => value?.ToString();

    [IgnoreInterface]
    public void Hidden() { }
}

public class App(IReportFormatter formatter)
{
    public string Title { get; } = formatter.Title ?? string.Empty;

    public string Formatted { get; } = formatter.Format(new Order(42)) ?? string.Empty;
}

public class Order(int id)
{
    public override string ToString() => $"Order #{id}";
}
// }
