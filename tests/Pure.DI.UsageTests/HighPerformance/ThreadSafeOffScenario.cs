/*
$v=true
$p=HighPerformance:12
$d=ThreadSafe Off for single-thread composition
$sa=ThreadSafe hint
$sa=Thread-safe overrides
$h=Pure.DI generates thread-safe code by default because composition instances are often shared. When a composition is created and used on one thread, such as inside a command-line import step, a game-loop setup phase, or a short-lived benchmark harness, the synchronization path can be disabled explicitly.
$h=This example builds a report import pipeline that is created, used, and discarded inside one job. `ThreadSafe = Off` removes generated locking for composition-owned cached instances, while the application keeps the single-threaded ownership rule at the boundary.
$f=Use this only when the composition instance is not shared across threads. If delegates, factories, or roots can be invoked concurrently, keep thread safety enabled or synchronize the critical factory section yourself with `ctx.Lock`.
$f=
$f=The legitimate cases are narrow and easy to check by eye: a command-line run, a single-threaded setup phase before workers start, a benchmark harness, and unit tests that create their own composition per test. The generated code drops the lock around cached singletons, which is measurable only when those singletons are resolved in a tight loop.
$f=>[!WARNING]
$f=>A composition created on one thread but reached from a `Task`, a timer callback or a UI dispatcher is not single-threaded. The failure mode is a torn or duplicated singleton that appears under load, not at startup.
$f=
$f=Prefer the default thread-safe generation for anything that lives longer than one call stack. If only one factory needs protection, keep `ThreadSafe` on and guard that factory with `ctx.Lock` - see [Factory with thread synchronization](factory-with-thread-synchronization.md) and [ThreadSafe hint](threadsafe-hint.md).
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.HighPerformance.ThreadSafeOffScenario;

using Shouldly;
using Xunit;
using static Hint;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Hint;
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
            .Hint(ThreadSafe, "Off")
            .Bind().As(Singleton).To<ImportCache>()
            .Bind<IImportJob>().To<ImportJob>()
            .Root<IImportJob>("Job");

        var composition = new Composition();
        var job = composition.Job;

        job.Import(["A-100", "A-100", "B-200"]).ShouldBe(2);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IImportJob
{
    int Import(IReadOnlyList<string> productCodes);
}

sealed class ImportJob(ImportCache cache) : IImportJob
{
    public int Import(IReadOnlyList<string> productCodes)
    {
        foreach (var code in productCodes)
        {
            cache.SeenCodes.Add(code);
        }

        return cache.SeenCodes.Count;
    }
}

sealed class ImportCache
{
    public HashSet<string> SeenCodes { get; } = [];
}
// }
