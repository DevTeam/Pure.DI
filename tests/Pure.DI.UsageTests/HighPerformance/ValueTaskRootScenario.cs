/*
$v=true
$p=HighPerformance:7
$d=ValueTask root
$sa=ValueTask
$sa=Async Root
$h=A root can return `ValueTask<T>` when the caller naturally awaits the result but most executions complete synchronously. This avoids allocating a `Task<T>` for the fast path while still allowing the same API to grow into asynchronous initialization later.
$h=The example models a feature flag snapshot used by request routing. The snapshot is normally available from an in-memory cache, so the composition root returns a completed `ValueTask<IFeatureSnapshot>` and the caller can await it without forcing a heap allocation for the common case.
$f=Use `ValueTask<T>` for roots that are awaited frequently and usually complete synchronously. Keep the usual `ValueTask<T>` rules: await it once, do not store it for later, and use `Task<T>` when the result is naturally shared or awaited multiple times.
$f=Pure.DI still generates regular strongly typed code; the performance benefit comes from choosing an allocation-friendly asynchronous shape at the root boundary.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.HighPerformance.ValueTaskRootScenario;

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
            .Bind<IFeatureSnapshot>().To<FeatureSnapshot>()
            .Root<ValueTask<IFeatureSnapshot>>("GetSnapshotAsync");

        var composition = new Composition();

        var snapshot = await composition.GetSnapshotAsync;
        snapshot.IsEnabled("checkout-v2").ShouldBeTrue();
// }
        snapshot.ShouldBeOfType<FeatureSnapshot>();
        composition.SaveClassDiagram();
    }
}

// {
interface IFeatureSnapshot
{
    bool IsEnabled(string name);
}

sealed class FeatureSnapshot : IFeatureSnapshot
{
    private readonly HashSet<string> _enabled =
    [
        "checkout-v2",
        "new-search"
    ];

    public bool IsEnabled(string name) => _enabled.Contains(name);
}
// }
