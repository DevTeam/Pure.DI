/*
$v=true
$p=6
$d=Tracking disposable instances per a composition root
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable InvertIf
namespace Pure.DI.UsageTests.Hints.TrackingDisposableInstances2Scenario;

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

internal static class Disposables
{
    public static readonly IDisposable Empty = new EmptyDisposable();
    
    public static IDisposable Combine(this Stack<IDisposable> disposables) =>
        new CombinedDisposable(disposables);

    private class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    private class CombinedDisposable(Stack<IDisposable> disposables)
        : IDisposable
    {
        public void Dispose()
        {
            while (disposables.TryPop(out var disposable))
            {
                disposable.Dispose();
            }
        }
    }
}

partial class Composition
{
    private readonly ConcurrentDictionary<int, Stack<IDisposable>> _disposables = new();
    
    private void Setup() =>
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
            .Bind().To(_ => 
                _disposables.TryRemove(Environment.CurrentManagedThreadId, out var disposables)
                    ? disposables.Combine()
                    : Disposables.Empty)
            
            .Root<(IService service, IDisposable combinedDisposables)>("Root");

    partial void OnNewInstance<T>(
        ref T value,
        object? tag,
        Lifetime lifetime)
    {
        if (value is IDisposable disposable)
        {
            var disposables = _disposables.GetOrAdd(
                Environment.CurrentManagedThreadId,
                _ => new Stack<IDisposable>());

            disposables.Push(disposable);
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
        
        root1.combinedDisposables.Dispose();
        
        // Checks that the disposable instances
        // associated with root1 have been disposed of
        root1.service.Dependency.IsDisposed.ShouldBeTrue();
        
        // Checks that the disposable instances
        // associated with root2 have not been disposed of
        root2.service.Dependency.IsDisposed.ShouldBeFalse();
        // }
        new Composition().SaveClassDiagram();
    }
}