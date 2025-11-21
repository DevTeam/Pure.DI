/*
$v=true
$p=7
$d=Async Enumerable
$h=Specifying `IAsyncEnumerable<T>` as the injection type allows instances of all bindings implementing type `T` to be injected in an asynchronous-lazy manner - the instances will be provided one at a time, in an order corresponding to the sequence of the bindings.
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
        // This hint indicates to not generate methods such as Resolve
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