/*
$v=true
$p=10
$d=Light roots
$h=Light roots optimize code generation by avoiding the creation of separate composition objects for each root. Instead, they share a common lightweight composition and use delegates to create instances. This is particularly useful for simple, frequently resolved roots where the overhead of generating separate compositions outweighs the benefits. Anonymous roots (roots without explicit names) are lightweight by default.
$f=>[!NOTE]
$f=>Light roots are ideal for simple services, factories, or utilities that don't require complex dependency graphs. They reduce generated code size and improve compilation time.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.LightRootScenario;

using Shouldly;
using Xunit;
using static RootKinds;

// {
//# using Pure.DI;
//# using static Pure.DI.RootKinds;
// }
public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        DI.Setup(nameof(Composition))
            // Infrastructure bindings with simple lifetimes
            .Bind().To<ConsoleLogger>()
            .Bind().To<MemoryCache>()
            .Bind().To<PrometheusMetrics>()
            .Bind().To<AppConfiguration>()
            .Bind().To<ApplicationService>()

            // Regular root for complex composition
            .Root<IApplicationService>("ApplicationService")

            // Named lightweight root
            .Root<IConfiguration>("Config", kind: Light)

            // Anonymous lightweight roots (lightweight by default)
            .Root<ILogger>()
            .Root<ICache>()
            .Root<IMetrics>();

        var composition = new Composition();
        var applicationService = composition.ApplicationService;
        var config = composition.Config;

        // Anonymous roots are resolved via the Resolve method
        var logger = composition.Resolve<ILogger>();
        var cache = composition.Resolve<ICache>();
        var metrics = composition.Resolve<IMetrics>();

        // Verify that all light roots return correct types
        logger.ShouldBeOfType<ConsoleLogger>();
        cache.ShouldBeOfType<MemoryCache>();
        metrics.ShouldBeOfType<PrometheusMetrics>();
        config.ShouldBeOfType<AppConfiguration>();

        // Light roots can be resolved independently without complex composition overhead
        var anotherLogger = composition.Resolve<ILogger>();
        anotherLogger.ShouldNotBeSameAs(logger);
// }
        composition.SaveClassDiagram();
    }
}

// {
// Application service with complex dependencies
interface IApplicationService;

class ApplicationService(
    ILogger logger,
    ICache cache,
    IMetrics metrics,
    IConfiguration config)
    : IApplicationService;

// Simple logger interface and implementation
interface ILogger;

class ConsoleLogger : ILogger;

// Simple cache interface and implementation
interface ICache;

class MemoryCache : ICache;

// Simple metrics interface and implementation
interface IMetrics;

class PrometheusMetrics : IMetrics;

// Simple configuration interface and implementation
interface IConfiguration;

class AppConfiguration : IConfiguration;
// }
