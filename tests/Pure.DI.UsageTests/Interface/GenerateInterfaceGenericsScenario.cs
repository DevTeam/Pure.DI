/*
$v=true
$p=9
$d=Generate interfaces with generics
$h=This example shows that generic members, nullable annotations, and events are preserved in the generated interface.
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

namespace Pure.DI.UsageTests.Interface.GenerateInterfaceGenericsScenario;

using System;
using Pure.DI.UsageTests;
using Pure.DI;
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
        // {
        DI.Setup(nameof(Composition))
            .Bind<IFormatter>().To<Formatter>()
            .Bind<App>().To<App>()
            .Root<App>(nameof(App));

        var composition = new Composition();
        var app = composition.App;

        app.Formatted.ShouldBe("demo");
        app.Title.ShouldBe("demo");
        // }

        composition.SaveClassDiagram();
    }
}

// {
public partial interface IFormatter;

[GenerateInterface]
public partial class Formatter : IFormatter
{
    public string? Title { get; set; } = "demo";

#pragma warning disable CS0067
    public event EventHandler? Changed;
#pragma warning restore CS0067

    public string? Format<T>(T value)
        where T : class
        => value?.ToString();

    [IgnoreInterface]
    public void Hidden() { }
}

public class App(IFormatter formatter)
{
    public string Title { get; } = formatter.Title ?? string.Empty;

    public string Formatted { get; } = formatter.Format("demo") ?? string.Empty;
}
// }
