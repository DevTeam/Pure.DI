/*
$v=true
$p=HighPerformance:0
$d=Method injection for a hot path
$sa=Span and ReadOnlySpan
$sa=Root arguments
$sa=Method injection
$h=Method injection is useful when a service has stable dependencies but the hot-path input changes on every call. Instead of storing request state in a heap object, pass the per-call value as a root argument and consume it immediately in an initialization method.
$h=The route matcher below has a singleton-like route table and receives a `ReadOnlySpan<char>` request path for each call. The generated root method passes the span into `Match(...)`, so the stack-only value stays inside the current call frame and is not stored in a field or property.
$f=This is a good shape for parsers, routers, protocol decoders, validation pipelines, and other code that reads request data immediately. Keep the method side-effect small and do not store `Span<T>`/`ReadOnlySpan<T>` in the created object; Pure.DI reports diagnostics when stack-only values are routed to storage-like injection sites.
$f=
$f=The practical gain is that the per-call input never becomes a field, so the object needs no allocation per request and no cache unfriendly copy. Method injection also keeps the constructor free of values that are not available when the graph is built.
$f=>[!NOTE]
$f=>Injection methods run in `[Ordinal]` order, so give the input-consuming method a number that puts it after any method it depends on. A parameterless combination of order rules silently changes behavior when methods are added.
$f=
$f=Prefer a root argument with method injection for per-call stack-only data, and a `Func<TArg, TResult>` only when the consumer must create several instances. See [Root arguments](root-arguments.md) when the value is a normal object rather than a span.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageTests.HighPerformance.MethodInjectionHotPathScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System;
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
            .Bind<IRouteTable>().To<RouteTable>()
            .Bind<IRouteMatcher>().To<RouteMatcher>()
            .RootArg<ReadOnlySpan<char>>("path")
            .Root<IRouteMatcher>("CreateMatcher");

        var composition = new Composition();

        var matcher = composition.CreateMatcher("/orders/42".AsSpan());
        matcher.Route.ShouldBe("orders");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IRouteTable
{
    string Find(ReadOnlySpan<char> path);
}

sealed class RouteTable : IRouteTable
{
    public string Find(ReadOnlySpan<char> path) =>
        path.StartsWith("/orders/".AsSpan(), StringComparison.Ordinal)
            ? "orders"
            : "not-found";
}

interface IRouteMatcher
{
    string Route { get; }
}

sealed class RouteMatcher(IRouteTable routeTable) : IRouteMatcher
{
    public string Route { get; private set; } = "";

    [Ordinal(0)]
    public void Match(ReadOnlySpan<char> path) =>
        Route = routeTable.Find(path);
}
// }
