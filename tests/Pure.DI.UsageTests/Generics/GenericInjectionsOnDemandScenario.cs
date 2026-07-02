/*
$v=true
$p=14
$d=Generic injections on demand
$h=On-demand creation via `Func<T>` works inside generic types too. `Distributor<T>` takes a `Func<IWorker<T>>` and calls it whenever it needs another worker — each call produces a new `Worker<T>` for the same type argument.
$h=Use this when the consumer, not the composition, decides how many instances to create and when.
$f=>[!NOTE]
$f=>Generic on-demand injection provides flexibility for creating instances with different type parameters as needed.
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