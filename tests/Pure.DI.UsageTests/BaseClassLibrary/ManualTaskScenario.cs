/*
$v=true
$p=4
$d=Manually started tasks
$h=By default, tasks are started automatically when they are injected. But you can override this behavior as shown in the example below. It is also recommended to add a binding for <c>CancellationToken</c> to be able to cancel the execution of a task.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Global
namespace Pure.DI.UsageTests.BCL.ManualTaskScenario;

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

class Service : IService
{
    private readonly Task<IDependency> _dependencyTask;

    public Service(Task<IDependency> dependencyTask)
    {
        _dependencyTask = dependencyTask;
        _dependencyTask.Start();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var dependency = await _dependencyTask;
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
            .Bind<Task<TT>>().To(ctx =>
            {
                ctx.Inject(ctx.Tag, out Func<TT> factory);
                ctx.Inject(out CancellationToken cancellationToken);
                return new Task<TT>(factory, cancellationToken);
            })
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("GetRoot")
            .Bind<CancellationTokenSource>().As(Lifetime.Singleton).To<CancellationTokenSource>()
            // Specifies to use CancellationToken from the composition root argument,
            // if not specified then CancellationToken.None will be used
            .RootArg<CancellationToken>("cancellationToken");

        var composition = new Composition();
        using var cancellationTokenSource = new CancellationTokenSource();
        
        // Creates a composition root with the CancellationToken passed to it
        var service = composition.GetRoot(cancellationTokenSource.Token);
        await service.RunAsync(cancellationTokenSource.Token);
// }            
        composition.SaveClassDiagram();
    }
}