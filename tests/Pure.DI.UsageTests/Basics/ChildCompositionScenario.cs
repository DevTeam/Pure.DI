/*
$v=true
$p=8
$d=Child composition
$h=Can use generated classes in hierarchy.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
namespace Pure.DI.UsageTests.Basics.ChildCompositionScenario;

using Shouldly;
using Xunit;

// {
internal interface IDependency
{
    bool IsDisposed { get; }
}

internal class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

internal interface IService
{
    IDependency Dependency { get; }
}

internal class Service : IService
{
    public Service(IDependency dependency)
    {
        Dependency = dependency;
    }

    public IDependency Dependency { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
        // FormatCode = Off
// {            
        DI.Setup("Composition")
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
        TestTools.SaveClassDiagram(composition, nameof(ChildCompositionScenario));
    }
}