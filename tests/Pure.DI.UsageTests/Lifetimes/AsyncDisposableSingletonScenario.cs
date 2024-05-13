/*
$v=true
$p=4
$d=Async disposable singleton
$h=If at least one of these objects implements the `IAsyncDisposable` interface, then the composition implements `IAsyncDisposable` as well. To dispose of all created singleton instances in an asynchronous manner, simply dispose of the composition instance in an asynchronous manner: 
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Lifetimes.AsyncDisposableSingletonScenario;

using Xunit;

// {
interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(IDependency dependency): IService
{
    public IDependency Dependency { get; } = dependency;
}
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
// {            
        DI.Setup(nameof(Composition))
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            .Bind().As(Lifetime.Singleton).To<Dependency>()
            .RootBind<IService>("Root").To<Service>();

        IDependency dependency;
        await using (var composition = new Composition())
        {
            var service = composition.Root;
            dependency = service.Dependency;
        }
            
        dependency.IsDisposed.ShouldBeTrue();
// }
        new Composition().SaveClassDiagram();
    }
}