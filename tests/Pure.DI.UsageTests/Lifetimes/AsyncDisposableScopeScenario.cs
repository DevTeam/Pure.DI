/*
$v=true
$p=9
$d=Async disposable scope
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario;

using Xunit;
using static Lifetime; 

// {
interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
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
    void Setup() =>
        DI.Setup(nameof(Composition))
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            .Bind().As(Scoped).To<Dependency>()
            
            // Session composition root
            .RootBind<IService>("SessionRoot").To<Service>()
            
            // Program composition root
            .Root<Program>("ProgramRoot");
}
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
// {            
        var composition = new Composition();
        var program = composition.ProgramRoot;
        
        // Creates session #1
        var session1 = program.CreateSession();
        var dependency1 = session1.SessionRoot.Dependency;
        var dependency12 = session1.SessionRoot.Dependency;
        
        // Checks the identity of scoped instances in the same session
        dependency1.ShouldBe(dependency12);
        
        // Creates session #2
        var session2 = program.CreateSession();
        var dependency2 = session2.SessionRoot.Dependency;
        
        // Checks that the scoped instances are not identical in different sessions
        dependency1.ShouldNotBe(dependency2);
        
        // Disposes of session #1
        await session1.DisposeAsync();
        // Checks that the scoped instance is finalized
        dependency1.IsDisposed.ShouldBeTrue();
        
        // Disposes of session #2
        await session2.DisposeAsync();
        // Checks that the scoped instance is finalized
        dependency2.IsDisposed.ShouldBeTrue();
// }
        composition.SaveClassDiagram();
    }
}