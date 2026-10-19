/*
$v=true
$p=13
$d=Async disposable scope
$sa=Scope
$sa=Async disposable singleton
$h=When scoped services hold resources that need asynchronous cleanup — network connections, streams, database sessions — implement `IAsyncDisposable` on them and dispose of the scope with `await scope.DisposeAsync()`.
$h=A scope is a class derived from the composition (here `Session`): each session gets its own `Scoped` instances, and disposing the session asynchronously disposes everything created within it.
$f=>[!NOTE]
$f=>Async disposable scope is essential for scenarios requiring proper async cleanup of scoped resources.
$f=
$f=A per-request session that holds a network stream, a database session or an open file is exactly this case: the scope ends when the request ends, and the release must await the network.
$f=Each session is a child composition, so scoped instances are unique per session and singletons stay shared with the parent. Disposing a session releases only what that session created.
$f=>[!WARNING]
$f=>A session that is dropped without `DisposeAsync` keeps its scoped resources alive until the GC runs finalizers. In a request pipeline, tie the release to the same place that ends the request, not to a background cleanup.
$f=
$f=Prefer `await using` inside an `async` method so the dispose happens on every exit path, including exceptions. When the scoped resources are synchronous, the plain [Scope](scope.md) variant avoids the extra state machine.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

// ReSharper disable PartialTypeWithSinglePart
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario;

using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
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

partial class Program(Func<Session> sessionFactory)
{
    public Session CreateSession() => sessionFactory();
}

partial class Composition
{
    static void Setup() =>
// }
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup()
            .Bind().As(Scoped).To<Dependency>()
            .Bind().To<Service>()

            // Session composition root
            .Root<IService>("SessionRoot")

            // Program composition root
            .Root<Program>("ProgramRoot");
}
// }