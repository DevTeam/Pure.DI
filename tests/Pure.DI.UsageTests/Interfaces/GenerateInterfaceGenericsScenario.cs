/*
$v=true
$p=3
$d=Generate interfaces with generics
$h=This example shows how generic members, nullable annotations, and events are preserved in a reporting scenario. The generated interface mirrors the member signatures, including the `where` constraint on `Format<T>`, so callers keep full type information through the abstraction.
$f=The example shows how to:
$f=- Generate an interface for generic members
$f=- Preserve nullable annotations
$f=- Preserve events and generic constraints
$f=
$f=A formatter, a serializer or a repository with generic methods is a natural fit: the contract can be generated from the class without losing the constraints that make the methods usable.
$f=Nullability is part of the generated signature, so `string?` in the class stays `string?` in the contract and a consumer that ignores it gets the usual warnings instead of a silent null.
$f=>[!NOTE]
$f=>Events are declared in the interface, so a consumer can subscribe through the abstraction. The class must still raise them; the generator does not add any plumbing.
$f=
$f=Prefer this for generic member signatures that are painful to repeat by hand, and mark members that should stay internal with `[IgnoreInterface]` as `Hidden` does here.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable CS0067
#pragma warning disable CA1822
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
        => value.ToString();

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
