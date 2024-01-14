/*
$v=true
$p=4
$d=Manually started tasks
$h=By default, tasks are started automatically when they are injected. But you can override this behavior as in the example below. It is recommended to use an argument of type <c>CancellationToken</c> to the composition root to be able to cancel the execution of a task. In this case, the composition root property is automatically converted to a method with a parameter of type <c>CancellationToken</c>.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.BCL.ManualTaskScenario;

using Xunit;

// {
interface IDependency { }

class Dependency : IDependency { }

interface IService
{
    Task RunAsync();
}

class Service : IService
{
    private readonly Task<IDependency> _dependencyTask;

    public Service(Task<IDependency> dependencyTask)
    {
        _dependencyTask = dependencyTask;
        _dependencyTask.Start();
    }

    public async Task RunAsync()
    {
        var dependency = await _dependencyTask;
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
            .Bind<Task<TT>>().To(ctx =>
            {
                ctx.Inject(ctx.Tag, out Func<TT> factory);
                ctx.Inject(out CancellationToken cancellationToken);
                return new Task<TT>(factory, cancellationToken);
            })
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("GetRoot");

        var composition = new Composition();
        var service = composition.GetRoot(CancellationToken.None);
        await service.RunAsync();
// }            
        composition.SaveClassDiagram();
    }
}