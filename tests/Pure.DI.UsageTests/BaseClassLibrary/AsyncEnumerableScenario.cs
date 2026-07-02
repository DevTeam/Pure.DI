/*
$v=true
$p=12
$d=Async Enumerable
$sa=Enumerable
$h=Specifying `IAsyncEnumerable<T>` as the injection type allows instances of all bindings implementing type `T` to be injected in an asynchronous-lazy manner - the instances will be provided one at a time, in an order corresponding to the sequence of the bindings.
$f=>[!NOTE]
$f=>IAsyncEnumerable<T> provides efficient lazy enumeration for scenarios where you need to process many instances without loading them all into memory at once.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.BCL.AsyncEnumerableScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind<IHealthCheck>().To<MemoryCheck>()
            .Bind<IHealthCheck>("External").To<ExternalServiceCheck>()
            .Bind<IHealthService>().To<HealthService>()

            // Composition root
            .Root<IHealthService>("HealthService");

        var composition = new Composition();
        var healthService = composition.HealthService;
        var checks = await healthService.GetChecksAsync();

        checks[0].ShouldBeOfType<MemoryCheck>();
        checks[1].ShouldBeOfType<ExternalServiceCheck>();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IHealthCheck;

class MemoryCheck : IHealthCheck;

class ExternalServiceCheck : IHealthCheck;

interface IHealthService
{
    Task<IReadOnlyList<IHealthCheck>> GetChecksAsync();
}

class HealthService(IAsyncEnumerable<IHealthCheck> checks) : IHealthService
{
    public async Task<IReadOnlyList<IHealthCheck>> GetChecksAsync()
    {
        var results = new List<IHealthCheck>();
        await foreach (var check in checks)
        {
            results.Add(check);
        }

        return results;
    }
}
// }