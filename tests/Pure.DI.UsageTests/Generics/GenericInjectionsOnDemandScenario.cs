/*
$v=true
$p=8
$d=Generic injections on demand
$sa=Injection on demand
$sa=Generic injections on demand with arguments
$h=On-demand creation via `Func<T>` works inside generic types too. `Distributor<T>` takes a `Func<IWorker<T>>` and calls it whenever it needs another worker — each call produces a new `Worker<T>` for the same type argument.
$h=Use this when the consumer, not the composition, decides how many instances to create and when.
$f=>[!NOTE]
$f=>Generic on-demand injection provides flexibility for creating instances with different type parameters as needed.
$f=
$f=A worker pool, a plugin list or a retry loop that needs as many instances as the data demands uses this shape. The consumer decides the count and the moment, and the factory keeps the graph - and any lifetime attached to it - intact.
$f=Each call is a separate resolution, so `Transient` and `PerResolve` dependencies are new per call while singletons stay shared. That is what makes the factory safe to call in a loop.
$f=>[!NOTE]
$f=>If the factory escapes the composition - stored in a long-lived object or handed to another thread - it keeps the composition reachable. Keep the factory's lifetime inside the scope that owns the consumer.
$f=
$f=Prefer this over injecting a collection when the number of instances is decided at run time. Use [Injections on demand with arguments](injections-on-demand-with-arguments.md) when the consumer also supplies per-instance data.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedTypeParameter
namespace Pure.DI.UsageTests.Generics.GenericInjectionsOnDemandScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Generic;
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
            .Bind().To<Worker<TT>>()
            .Bind().To<Distributor<TT>>()

            // Composition root
            .Root<IDistributor<int>>("Root");

        var composition = new Composition();
        var distributor = composition.Root;

        // Check that the distributor has created 2 workers
        distributor.Workers.Count.ShouldBe(2);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IWorker<T>;

class Worker<T> : IWorker<T>;

interface IDistributor<T>
{
    IReadOnlyList<IWorker<T>> Workers { get; }
}

class Distributor<T>(Func<IWorker<T>> workerFactory) : IDistributor<T>
{
    public IReadOnlyList<IWorker<T>> Workers { get; } =
    [
        // Creates the first instance of the worker
        workerFactory(),
        // Creates the second instance of the worker
        workerFactory()
    ];
}
// }