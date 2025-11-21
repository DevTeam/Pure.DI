/*
$v=true
$p=8
$d=Accumulators
$h=Accumulators allow you to accumulate instances of certain types and lifetimes.
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
        // This hint indicates to not generate methods such as Resolve
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