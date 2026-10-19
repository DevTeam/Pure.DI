/*
$v=true
$p=104
$d=Tracking async disposable instances per a composition root
$sa=Tracking disposable instances per a composition root
$sa=Tracking async disposable instances in delegates
$h=Declaring a root as `Root<Owned<T>>` gives every root instance ownership of its own dependency graph, including `IAsyncDisposable` dependencies. Calling `DisposeAsync()` on one `Owned<T>` asynchronously disposes only the instances created for that root, leaving graphs obtained from other root accesses untouched.
$f=A per-request scope in a server is exactly this: create the owner, serve the request, `await DisposeAsync()` in a `finally`, and the connection is returned even when the request fails.
$f=Only resources created while resolving this root belong to it, so two owners retrieved from the same composition never share a transient connection.
$f=Dispose the owner asynchronously when the graph contains `IAsyncDisposable` resources; the synchronous `Dispose()` on such a graph is blocked waiting for it, so prefer `DisposeAsync()` where the framework allows it.
$f=>[!NOTE]
$f=>Async disposable tracking ensures proper async cleanup of all disposable instances within a composition scope.
$f=
$f=Prefer the composition itself for resources that must live until shutdown, and [tracking disposable instances in delegates](tracking-disposable-instances-in-delegates.md) when the owners are created on demand inside a service.
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers

namespace Pure.DI.UsageTests.Advanced.TrackingAsyncDisposableScenario;

using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
// {
        var composition = new Composition();
        // Creates two independent roots (queries), each with its own dependency graph
        var query1 = composition.Query;
        var query2 = composition.Query;

        // Disposes of the second query
        await query2.DisposeAsync();

        // Checks that the connection associated with the second query has been closed
        query2.Value.Connection.IsDisposed.ShouldBeTrue();

        // At the same time, the connection of the first query remains active
        query1.Value.Connection.IsDisposed.ShouldBeFalse();

        // Disposes of the first query
        await query1.DisposeAsync();

        // Now the first connection is also closed
        query1.Value.Connection.IsDisposed.ShouldBeTrue();
// }
        new Composition().SaveClassDiagram();
    }
}

// {
// Interface for a resource requiring asynchronous disposal (e.g., DB)
interface IDbConnection
{
    bool IsDisposed { get; }
}

class DbConnection : IDbConnection, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IQuery
{
    public IDbConnection Connection { get; }
}

class Query(IDbConnection connection) : IQuery
{
    public IDbConnection Connection { get; } = connection;
}

partial class Composition
{
    static void Setup() =>
// }
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
        // SystemThreadingLock = Off
// {
        DI.Setup()
            .Bind().To<DbConnection>()
            .Bind().To<Query>()

            // A special composition root 'Owned' that allows
            // managing the lifetime of IQuery and its dependencies
            .Root<Owned<IQuery>>("Query");
}
// }