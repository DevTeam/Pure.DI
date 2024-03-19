/*
$v=true
$p=6
$d=Tracking disposable instances per a composition root
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario;

using System.Collections.Concurrent;
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
    private ConcurrentDictionary<int, List<IDisposable>> _disposables = [];

    private void Setup() =>
        DI.Setup(nameof(Composition))
            // Specifies to call the partial method OnNewInstance
            // when an instance is created
            .Hint(Hint.OnNewInstance, "On")
            
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<Owned<IService>>("Root");
    
    partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime)
    {
        if (value is IOwned || value is not IDisposable disposable
            || lifetime is Lifetime.Singleton or Lifetime.Scoped)
        {
            return;
        }

        _disposables.GetOrAdd(Environment.CurrentManagedThreadId, _ => [])
            .Add(disposable);
    }
    
    public interface IOwned;
    
    public readonly struct Owned<T>: IDisposable, IOwned
    {
        public readonly T Value;
        private readonly List<IDisposable> _disposable;

        public Owned(T value, Composition composition)
        {
            Value = value;
            _disposable = composition._disposables.TryRemove(Environment.CurrentManagedThreadId, out var disposables)
                ? disposables
                : [];
            
            composition._disposables = [];
        }

        public void Dispose()
        {
            _disposable.Reverse();
            _disposable.ForEach(i => i.Dispose());
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

        var root1 = composition.Root;
        var root2 = composition.Root;
        
        root2.Dispose();
        
        // Checks that the disposable instances
        // associated with root1 have been disposed of
        root2.Value.Dependency.IsDisposed.ShouldBeTrue();
        
        // Checks that the disposable instances
        // associated with root2 have not been disposed of
        root1.Value.Dependency.IsDisposed.ShouldBeFalse();
        
        root1.Dispose();
        
        // Checks that the disposable instances
        // associated with root2 have been disposed of
        root1.Value.Dependency.IsDisposed.ShouldBeTrue();
        // }
        new Composition().SaveClassDiagram();
    }
}