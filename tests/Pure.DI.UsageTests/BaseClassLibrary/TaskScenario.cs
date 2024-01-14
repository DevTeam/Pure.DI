/*
$v=true
$p=4
$d=Task
$h=By default, tasks are started automatically when they are injected. The composition root property is automatically transformed into a method with a parameter of type <c>CancellationToken</c>.
$h=To start a task, an instance of type <c>TaskFactory<T></c> is used, with default settings:
$h=
$h=- TaskScheduler.Default
$h=- TaskCreationOptions.None
$h=- TaskContinuationOptions.None
$h=
$h=But you can always override them, as in the example below for <c>TaskScheduler.Current</c>.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.BCL.TaskScenario;

using Xunit;

// {
interface IDependency { }

class Dependency : IDependency { }

interface IService
{
    Task RunAsync();
}

class Service(Task<IDependency> dependencyTask) : IService
{
    public async Task RunAsync()
    {
        var dependency = await dependencyTask;
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
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("GetRoot")
            // Overrides TaskScheduler.Default if necessary
            .Bind<TaskScheduler>().To(_ => TaskScheduler.Current);

        var composition = new Composition();
        var service = composition.GetRoot(CancellationToken.None);
        await service.RunAsync();
// }            
        composition.SaveClassDiagram();
    }
}