/*
$v=true
$p=3
$d=Disposable Singleton
$h=To dispose all created singleton instances, simply dispose the composition instance:
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
namespace Pure.DI.UsageTests.Lifetimes.DisposableSingletonScenario;

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
    public IDependency Dependency { get; }
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

        IDependency dependency;
        using (var composition = new Composition())
        {
            var service = composition.Root;
            dependency = service.Dependency;
        }
            
        dependency.IsDisposed.ShouldBeTrue();
// }
        TestTools.SaveClassDiagram(new Composition(), nameof(DisposableSingletonScenario));
    }
}