/*
$v=true
$p=4
$d=Scope
$h=A _scope_ scenario can be easily implemented with singleton instances and child composition:
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local
namespace Pure.DI.UsageTests.Lifetimes.ScopeScenario;

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
    public IDependency Dependency => dependency;
}

interface ISession : IDisposable
{
    IService SessionRoot { get; }
}

class Session: Composition, ISession;

class Program(Func<ISession> sessionFactory)
{
    public ISession CreateSession() => sessionFactory();
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            // This is actually a singleton
            // that will be created in the scope of the session instance.
            .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>().To<Service>()
            .Bind<ISession>().To<Session>()
            .Root<IService>("SessionRoot")
            .Root<Program>("ProgramRoot");
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        var composition = new Composition();
        var programRoot = composition.ProgramRoot;
        
        // Creates session #1
        var session1 = programRoot.CreateSession();
        var dependencyInSession1 = session1.SessionRoot.Dependency;
        dependencyInSession1.ShouldBe(session1.SessionRoot.Dependency);
        
        // Creates session #2
        using var session2 = programRoot.CreateSession();
        var dependencyInSession2 = session2.SessionRoot.Dependency;
        dependencyInSession1.ShouldNotBe(dependencyInSession2);

        // Disposes of session #1
        session1.Dispose();
        dependencyInSession1.IsDisposed.ShouldBeTrue();
        
        // Session #2 is still not finalized
        session2.SessionRoot.Dependency.IsDisposed.ShouldBeFalse();
        
        // Disposes of session #1
        session2.Dispose();
        dependencyInSession2.IsDisposed.ShouldBeTrue();
// }
        new Composition().SaveClassDiagram();
    }
}