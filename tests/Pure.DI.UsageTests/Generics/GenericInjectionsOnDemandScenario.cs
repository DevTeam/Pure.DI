/*
$v=true
$p=14
$d=Generic injections on demand
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
        // This hint indicates to not generate methods such as Resolve
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