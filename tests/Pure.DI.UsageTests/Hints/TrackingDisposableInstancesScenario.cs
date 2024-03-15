/*
$v=true
$p=6
$d=Tracking disposable instances
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Hints.TrackingDisposableInstancesScenario;

using Xunit;

// {
interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    public event Action<IDisposable> OnNewDisposable; 
    
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            // Specifies to call a partial method
            // named OnNewInstance when an instance is created
            .Hint(Hint.OnNewInstance, "On")
            
            // Specifies to call the partial method
            // only for instances with lifetime
            // Transient, PerResolve and PerBlock
            .Hint(
                Hint.OnNewInstanceLifetimeRegularExpression,
                "Transient|PerResolve|PerBlock")

            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<IService>("Root");

    partial void OnNewInstance<T>(
        ref T value,
        object? tag,
        Lifetime lifetime)
    {
        if (value is IDisposable disposable
            && OnNewDisposable is {} onNewDisposable)
        {
            onNewDisposable(disposable);
        }
    }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        var composition = new Composition();
        
        // Tracking disposable instances within a composition
        var disposables = new Stack<IDisposable>();
        composition.OnNewDisposable += disposable =>
            disposables.Push(disposable);
        
        var service = composition.Root;
        disposables.Count.ShouldBe(1);
        
        // Disposal of instances in reverse order
        while (disposables.TryPop(out var disposable))
        {
            disposable.Dispose();
        }
        
        // Verifies that the disposable instance
        // has been disposed of
        service.Dependency.IsDisposed.ShouldBeTrue();
        // }
        new Composition().SaveClassDiagram();
    }
}