/*
$v=true
$p=4
$d=Scope
$h=A "scope" scenario can be easily implemented with singleton instances and child composition:
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.UsageTests.Lifetimes.ScopeScenario;

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

internal interface ISession: IDisposable
{
    IService Service { get; }
}

internal class Session : ISession
{
    private readonly Composition _composition;

    // To make a composition type injectable, don't forget to create a partial class for composition
    public Session(Composition composition)
    {
        // Creates child container that represents a "scope" for this session
        _composition = new Composition(composition);

        // You must be careful not to use the "Service" root before the session is created
        // otherwise one instance will be shared across all sessions
        Service = _composition.Service;
    }

    public IService Service { get; }
    
    public void Dispose() => _composition.Dispose();
}

internal partial class Composition
{
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
// {            
        DI.Setup("Composition")
            // In a fact it is "scoped" singleton here
            .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Service")
            .Bind<ISession>().To<Session>().Root<ISession>("Session");

        using var composition = new Composition();
        
        var session1 = composition.Session;
        session1.Service.Dependency.ShouldBe(session1.Service.Dependency);
        
        using var session2 = composition.Session;
        session1.Service.Dependency.ShouldNotBe(session2.Service.Dependency);
        
        session1.Dispose();
        session1.Service.Dependency.IsDisposed.ShouldBeTrue();
        session2.Service.Dependency.IsDisposed.ShouldBeFalse();
// }
        TestTools.SaveClassDiagram(new Composition(), nameof(ScopeScenario));
    }
}