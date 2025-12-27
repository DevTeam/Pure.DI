/*
$v=true
$p=3
$d=Task
$h=By default, tasks are started automatically when they are injected. It is recommended to use an argument of type <c>CancellationToken</c> to the composition root to be able to cancel the execution of a task. In this case, the composition root property is automatically converted to a method with a parameter of type <c>CancellationToken</c>. To start a task, an instance of type <c>TaskFactory<T></c> is used, with default settings:
$h=
$h=- CancellationToken.None
$h=- TaskScheduler.Default
$h=- TaskCreationOptions.None
$h=- TaskContinuationOptions.None
$h=
$h=But you can always override them, as in the example below for <c>TaskScheduler.Current</c>.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedVariable
namespace Pure.DI.UsageTests.BCL.TaskScenario;

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
            .Hint(Hint.Resolve, "Off")
            // Overrides TaskScheduler.Default if necessary
            .Bind<TaskScheduler>().To(() => TaskScheduler.Current)
            // Specifies to use CancellationToken from the composition root argument,
            // if not specified, then CancellationToken.None will be used
            .RootArg<CancellationToken>("cancellationToken")
            .Bind<IDataService>().To<DataService>()
            .Bind<ICommand>().To<LoadDataCommand>()

            // Composition root
            .Root<ICommand>("GetCommand");

        var composition = new Composition();
        using var cancellationTokenSource = new CancellationTokenSource();

        // Creates a composition root with the CancellationToken passed to it
        var command = composition.GetCommand(cancellationTokenSource.Token);
        await command.ExecuteAsync(cancellationTokenSource.Token);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDataService
{
    ValueTask<string[]> GetItemsAsync(CancellationToken cancellationToken);
}

class DataService : IDataService
{
    public ValueTask<string[]> GetItemsAsync(CancellationToken cancellationToken) =>
        new(["Item1", "Item2"]);
}

interface ICommand
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

class LoadDataCommand(Task<IDataService> dataServiceTask) : ICommand
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Simulating some processing before needing the dependency
        await Task.Delay(1, cancellationToken);

        // The dependency is resolved asynchronously, so we await it here.
        // This allows the dependency to be created in parallel with the execution of this method.
        var dataService = await dataServiceTask;
        var items = await dataService.GetItemsAsync(cancellationToken);
    }
}
// }