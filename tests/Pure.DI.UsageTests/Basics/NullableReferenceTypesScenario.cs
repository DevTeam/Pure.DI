/*
$v=true
$p=26
$d=Nullable reference types
$sa=Default values
$h=Pure.DI preserves nullable reference type annotations when it reads dependency contracts, builds the graph, and generates composition members.
$h=Use nullable dependencies for values that are allowed to be absent. A nullable root or composition argument does not get a generated null check, while a non-null reference argument still does.
$h=A non-null binding can satisfy a nullable dependency request. This is useful for optional constructor parameters, nullable factory results, and nullable collection elements.
$h=>[!TIP]
$h=>`T?` means that the consumer can handle `null`; it does not mean that a missing binding is ignored. If no binding or auto-binding can provide the type, Pure.DI still reports the graph error.
$h=>[!NOTE]
$h=>When a nullable reference type is used as a generic argument, the generic type must allow nullable arguments. For example, prefer `where T : class?` over `where T : class` for contracts such as `IBox<string?>`; otherwise the C# compiler reports a nullable constraint warning before Pure.DI analyzes the graph.
$r=Shouldly
$f=Limitations: nullable annotations describe compile-time contracts. They are not runtime validation rules and do not replace explicit domain validation.
$f=Common pitfalls:
$f=- Using `T?` to hide a missing binding instead of modelling an optional value.
$f=- Forgetting tags for nullable primitive values when several values of the same type exist.
$f=- Assuming `IEnumerable<T?>` changes the lifetime of elements; lifetime still comes from the matched bindings.
$f=- Declaring generic contracts with `where T : class` and then consuming them as `T?`; use a nullable-aware constraint such as `where T : class?` when nullable generic arguments are valid.
$f=See also: [Composition arguments](composition-arguments.md), [Root arguments](root-arguments.md), [Injection on demand](injection-on-demand.md).
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.NullableReferenceTypesScenario;

using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Generic;
//# using System.Linq;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Root arguments make Resolve unusable, so disable Resolve generation
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Hint(Hint.Resolve, "Off")
            .Bind<IDatabase>().To<Database>()
            .Bind<IReportService>().To<ReportService>()

            // Nullable composition argument: no generated null check
            .Arg<string?>("defaultTitle", "title")

            // Nullable root argument: no generated null check
            .RootArg<string?>("connectionString", "connection")

            // Composition root
            .Root<IReportService>("CreateReportService");

        var composition = new Composition(defaultTitle: null);
        var reportService = composition.CreateReportService(connectionString: null);

        reportService.DefaultTitle.ShouldBeNull();
        reportService.ConnectionString.ShouldBeNull();
        reportService.OptionalDatabase.ShouldNotBeNull();
        reportService.Databases.Count.ShouldBe(1);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDatabase;

class Database : IDatabase;

interface IReportService
{
    string? DefaultTitle { get; }

    string? ConnectionString { get; }

    IDatabase? OptionalDatabase { get; }

    IReadOnlyList<IDatabase?> Databases { get; }
}

class ReportService(
    [Tag("title")] string? defaultTitle,
    [Tag("connection")] string? connectionString,
    IDatabase? optionalDatabase,
    IEnumerable<IDatabase?> databases)
    : IReportService
{
    public string? DefaultTitle { get; } = defaultTitle;

    public string? ConnectionString { get; } = connectionString;

    public IDatabase? OptionalDatabase { get; } = optionalDatabase;

    public IReadOnlyList<IDatabase?> Databases { get; } = databases.ToList();
}
// }
