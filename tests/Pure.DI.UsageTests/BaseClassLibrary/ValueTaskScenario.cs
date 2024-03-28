/*
$v=true
$p=3
$d=ValueTask
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.BCL.ValueTaskScenario;

using Xunit;

// {
interface IDependency
{
    ValueTask DoSomething();
}

class Dependency : IDependency
{
    public ValueTask DoSomething() => ValueTask.CompletedTask;
}

interface IService
{
    ValueTask RunAsync();
}

class Service(ValueTask<IDependency> dependencyTask) : IService
{
    public async ValueTask RunAsync()
    {
        var dependency = await dependencyTask;
        await dependency.DoSomething();
    }
}
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
// {            
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            
            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        await service.RunAsync();
// }            
        composition.SaveClassDiagram();
    }
}