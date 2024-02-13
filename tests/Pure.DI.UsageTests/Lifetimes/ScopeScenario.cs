/*
$v=true
$p=1
$d=Scope
$h=The _Scoped_ lifetime ensures that there will be a single instance of the dependency for each scope.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Lifetimes.ScopeScenario;

using Xunit;
using static Lifetime; 

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
    public IDependency Dependency => dependency;
}

// Implements a session
class Session(Composition composition) : Composition(composition);

class Program(Func<Session> sessionFactory)
{
    public Session CreateSession() => sessionFactory();
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().As(Scoped).To<Dependency>()
            .Bind<IService>().To<Service>()
            // Session composition root
            .Root<IService>("SessionRoot")
            // Program composition root
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
        var program = composition.ProgramRoot;
        
        // Creates session #1
        var session1 = program.CreateSession();
        var dependency1 = session1.SessionRoot.Dependency;
        
        // Creates session #2
        var session2 = program.CreateSession();
        var dependency2 = session2.SessionRoot.Dependency;
        
        // Checks that the scoped instances are not identical in different sessions
        dependency1.ShouldNotBe(dependency2);
        
        // Disposes of session #1
        session1.Dispose();
        // Checks that the scoped instance is finalized
        dependency1.IsDisposed.ShouldBeTrue();
        
        // Disposes of session #2
        session2.Dispose();
        // Checks that the scoped instance is finalized
        dependency2.IsDisposed.ShouldBeTrue();
// }
        composition.SaveClassDiagram();
    }
}