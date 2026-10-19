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
$f=
$f=Use it for a root that is read on a request path and is backed by an in-memory snapshot, a cached configuration, or any value that is already there. The call then completes synchronously and no `Task` object is needed.
$f=>[!WARNING]
$f=>A single `ValueTask<T>` instance must be awaited once. Do not store it in a field, await it twice, or hand it to two consumers; for a shared result, await it once and pass the value.
$f=
$f=Prefer `ValueTask<T>` for a root that usually completes synchronously and `Task<T>` when the work is genuinely asynchronous or the result is awaited in several places. See [ValueTask](valuetask.md) for the general guidance and [Async root](async-root.md) for a `Task`-based root.
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
