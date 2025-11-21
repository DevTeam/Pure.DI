/*
$v=true
$p=102
$d=Tracking async disposable instances per a composition root
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
        // This hint indicates to not generate methods such as Resolve
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