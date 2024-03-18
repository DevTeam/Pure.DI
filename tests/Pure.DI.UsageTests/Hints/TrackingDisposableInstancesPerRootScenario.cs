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

readonly struct Owned<T>(
    T value,
    List<IDisposable> disposables)
    : IDisposable
{
    public T Value { get; } = value;

    public void Dispose()
    {
        disposables.Reverse();
        disposables.ForEach(i => i.Dispose());
    }
}

partial class Composition
{
    private List<IDisposable> _disposables = [];

    private void Setup() =>
        DI.Setup(nameof(Composition))
            // Specifies to call the partial method OnNewInstance
            // when an instance is created
            .Hint(Hint.OnNewInstance, "On")
            
            // Specifies to call the partial method OnNewInstance
            // only for instances with lifetime
            // Transient, PerResolve and PerBlock
            .Hint(
                Hint.OnNewInstanceLifetimeRegularExpression,
                "Transient|PerResolve|PerBlock")
            
            // Specifies to call the partial method OnNewInstance
            // for instances other than Composition.Owned<T>
            .Hint(
                Hint.OnNewInstanceImplementationTypeNameRegularExpression,
                "^((?!Owned<).)*$")
            
            .Bind().To(ctx =>
            {
                ctx.Inject(ctx.Tag, out TT value);
                var disposables = _disposables;
                _disposables = [];
                return new Owned<TT>(value, disposables);
            })

            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<Owned<IService>>("Root");
    
    partial void OnNewInstance<T>(
        ref T value,
        object? tag,
        Lifetime lifetime)
    {
        if (value is not IDisposable disposable) return;
        _disposables.Add(disposable);
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