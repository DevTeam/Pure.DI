/*
$v=true
$p=23
$d=Accumulators
$sa=Tracking disposable instances per a composition root
$h=Accumulators allow you to accumulate instances of certain types and lifetimes: `Accumulate<T, TAccumulator>(...)` adds every created `T` whose lifetime is in the list to a `TAccumulator` that needs only a parameterless constructor and an `Add(T)` method.
$h=Use this when you need an aggregated view of created dependencies (for diagnostics, telemetry, or registries) instead of a single injected instance, and when the registry should not be threaded through constructors by hand.
$h=The accumulator is an ordinary dependency of the composition, so a root returns it like any other service.
$f=In a real project this is the shape of a start-up snapshot: collect the telemetry sources, plug-ins, handlers or validators the graph actually built, then hand the collected list to a diagnostics page or a health check.
$f=Limitations: accumulation order depends on object creation order in the graph, so do not treat it as a stable business ordering; sort or key the result before you expose it.
$f=The dependencies of a `Singleton` or `Scoped` instance belong to that shared instance, so `Transient`, `PerBlock` and `PerResolve` accumulators do not collect them.
$f=The accumulator itself is a `PerBlock` dependency: it is built for the root access that asks for it and describes the objects created in that access, so request it from the same root as the objects you want to observe.
$f=Prefer [Enumerable](enumerable.md) when you only need every implementation injected as a collection, and a `Singleton` collector service when the view must outlive a single root access.
$f=See also: [Enumerable](enumerable.md), [Lifetimes](transient.md).
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.AccumulatorScenario;

using Shouldly;
using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
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
            // Accumulates all instances implementing ITelemetrySource
            // into a collection of type TelemetryRegistry.
            // The accumulation applies to Transient and Singleton lifetimes.
            .Accumulate<ITelemetrySource, TelemetryRegistry>(Transient, Singleton)

            // Infrastructure bindings
            .Bind<IDataSource>().As(PerBlock).To<SqlDataSource>()
            .Bind<IDataSource>(Tag.Type).To<SqlDataSource>()
            .Bind<IDataSource>(Tag.Type).As(Singleton).To<NetworkDataSource>()
            .Bind<IDashboard>().To<Dashboard>()

            // Composition root
            .Root<(IDashboard dashboard, TelemetryRegistry registry)>("Root");

        var composition = new Composition();
        var (dashboard, registry) = composition.Root;

        // Checks that all telemetry sources have been collected
        registry.Count.ShouldBe(3);
        // The order of accumulation depends on the order of object creation in the graph
        registry[0].ShouldBeOfType<NetworkDataSource>();
        registry[1].ShouldBeOfType<SqlDataSource>();
        registry[2].ShouldBeOfType<Dashboard>();
// }
        composition.SaveClassDiagram();
    }
}

// {
// Represents a component that produces telemetry data
interface ITelemetrySource;

// Accumulator for collecting all telemetry sources in the object graph
class TelemetryRegistry : List<ITelemetrySource>;

// Abstract data source interface
interface IDataSource;

// SQL database implementation acting as a telemetry source
class SqlDataSource : IDataSource, ITelemetrySource;

// Network data source implementation acting as a telemetry source
class NetworkDataSource : IDataSource, ITelemetrySource;

// Dashboard interface
interface IDashboard;

// Dashboard implementation aggregating data from sources
class Dashboard(
    [Tag(typeof(SqlDataSource))] IDataSource primaryDb,
    [Tag(typeof(NetworkDataSource))] IDataSource externalApi,
    IDataSource fallbackDb)
    : IDashboard, ITelemetrySource;
// }
