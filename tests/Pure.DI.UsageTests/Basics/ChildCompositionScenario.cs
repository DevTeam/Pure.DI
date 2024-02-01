/*
$v=true
$p=8
$d=Child composition
$h=Can use generated classes in hierarchy.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Basics.ChildCompositionScenario;

using Shouldly;
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
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        IService serviceFromChild;
        using (var childComposition = new Composition(composition))
        {
            serviceFromChild = childComposition.Root;
        }
        
        serviceFromChild.Dependency.IsDisposed.ShouldBeTrue();
        
        var service = composition.Root;
        using (var childComposition = new Composition(composition))
        {
            childComposition.Root.Dependency.ShouldBe(service.Dependency);
        }
// }            
        service.ShouldBeOfType<Service>();
        composition.SaveClassDiagram();
    }
}