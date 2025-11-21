/*
$v=true
$p=101
$d=Tracking disposable instances with different lifetimes
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable UnusedMemberInSuper.Global

namespace Pure.DI.UsageTests.Advanced.TrackingDisposableWithDifferentLifetimesScenario;

using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        var composition = new Composition();
        var queryHandler1 = composition.QueryHandler;
        var queryHandler2 = composition.QueryHandler;

        // The exclusive connection is created for each handler
        queryHandler1.ExclusiveConnection.ShouldNotBe(queryHandler2.ExclusiveConnection);

        // The shared connection is the same for all handlers
        queryHandler1.SharedConnection.ShouldBe(queryHandler2.SharedConnection);

        // Disposing the second handler
        queryHandler2.Dispose();

        // Checks that the exclusive connection
        // associated with queryHandler2 has been disposed of
        queryHandler2.ExclusiveConnection.IsDisposed.ShouldBeTrue();

        // But the shared connection is still alive (not disposed)
        // because it is a Singleton and shared with other consumers
        queryHandler2.SharedConnection.IsDisposed.ShouldBeFalse();

        // Checks that the connections associated with root1 
        // are not affected by queryHandler2 disposal
        queryHandler1.ExclusiveConnection.IsDisposed.ShouldBeFalse();
        queryHandler1.SharedConnection.IsDisposed.ShouldBeFalse();

        // Disposing the first handler
        queryHandler1.Dispose();

        // Its exclusive connection is now disposed
        queryHandler1.ExclusiveConnection.IsDisposed.ShouldBeTrue();

        // The shared connection is STILL alive
        queryHandler1.SharedConnection.IsDisposed.ShouldBeFalse();

        // Disposing the composition root container
        // This should dispose all Singletons
        composition.Dispose();

        // Now the shared connection is disposed
        queryHandler1.SharedConnection.IsDisposed.ShouldBeTrue();
// }
        new Composition().SaveClassDiagram();
    }
}

// {
interface IConnection
{
    bool IsDisposed { get; }
}

class Connection : IConnection, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IQueryHandler
{
    public IConnection ExclusiveConnection { get; }

    public IConnection SharedConnection { get; }
}

class QueryHandler(
    Func<Owned<IConnection>> connectionFactory,
    [Tag("Shared")] Func<Owned<IConnection>> sharedConnectionFactory)
    : IQueryHandler, IDisposable
{
    private readonly Owned<IConnection> _exclusiveConnection = connectionFactory();
    private readonly Owned<IConnection> _sharedConnection = sharedConnectionFactory();

    public IConnection ExclusiveConnection => _exclusiveConnection.Value;

    public IConnection SharedConnection => _sharedConnection.Value;

    public void Dispose()
    {
        // Disposes the owned instances.
        // For the exclusive connection (Transient), this disposes the actual connection.
        // For the shared connection (Singleton), this just releases the ownership
        // but does NOT dispose the underlying singleton instance until the container is disposed.
        _exclusiveConnection.Dispose();
        _sharedConnection.Dispose();
    }
}

partial class Composition
{
    static void Setup() =>
// }
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup()
            .Bind().To<Connection>()
            .Bind("Shared").As(Lifetime.Singleton).To<Connection>()
            .Bind().To<QueryHandler>()

            // Composition root
            .Root<QueryHandler>("QueryHandler");
}
// }