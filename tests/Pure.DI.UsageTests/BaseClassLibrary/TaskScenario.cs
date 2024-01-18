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
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Global
namespace Pure.DI.UsageTests.BCL.TaskScenario;

using Xunit;

// {
interface IDependency
{
    ValueTask DoSomething(CancellationToken cancellationToken);
}

class Dependency : IDependency
{
    public ValueTask DoSomething(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

interface IService
{
    Task RunAsync(CancellationToken cancellationToken);
}

class Service(Task<IDependency> dependencyTask) : IService
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var dependency = await dependencyTask;
        await dependency.DoSomething(cancellationToken);
    }
}
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
// {            
        DI.Setup("Composition")
            .Hint(Hint.Resolve, "Off")
            // Overrides TaskScheduler.Default if necessary
            .Bind<TaskScheduler>().To(_ => TaskScheduler.Current)
            // Specifies to use CancellationToken from the composition root argument,
            // if not specified then CancellationToken.None will be used
            .RootArg<CancellationToken>("cancellationToken")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("GetRoot");

        var composition = new Composition();
        using var cancellationTokenSource = new CancellationTokenSource();

        // Creates a composition root with the CancellationToken passed to it
        var service = composition.GetRoot(cancellationTokenSource.Token);
        await service.RunAsync(cancellationTokenSource.Token);
// }            
        composition.SaveClassDiagram();
    }
}