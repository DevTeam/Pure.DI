/*
$v=true
$p=103
$d=Tracking async disposable instances in delegates
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable UnusedMemberInSuper.Global

namespace Pure.DI.UsageTests.Advanced.TrackingAsyncDisposableInDelegatesScenario;

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
        var queryService1 = composition.QueryService;
        var queryService2 = composition.QueryService;

        await queryService2.DisposeAsync();

        // Checks that the disposable instances
        // associated with queryService2 have been disposed of
        queryService2.Connection.IsDisposed.ShouldBeTrue();

        // Checks that the disposable instances
        // associated with queryService1 have not been disposed of
        queryService1.Connection.IsDisposed.ShouldBeFalse();

        await queryService1.DisposeAsync();

        // Checks that the disposable instances
        // associated with queryService1 have been disposed of
        queryService1.Connection.IsDisposed.ShouldBeTrue();
// }
        new Composition().SaveClassDiagram();
    }
}

// {
interface IConnection
{
    bool IsDisposed { get; }
}

class DbConnection : IConnection, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IQueryService
{
    public IConnection Connection { get; }
}

class QueryService(Func<Owned<IConnection>> connectionFactory)
    : IQueryService, IAsyncDisposable
{
    // The Owned<T> generic type allows you to manage the lifetime of a dependency
    // explicitly. In this case, the QueryService creates the connection
    // using a factory and takes ownership of it.
    private readonly Owned<IConnection> _connection = connectionFactory();

    public IConnection Connection => _connection.Value;

    public ValueTask DisposeAsync()
    {
        // When the service is disposed, it also disposes of the connection it owns
        return _connection.DisposeAsync();
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
            .Bind<IConnection>().To<DbConnection>()
            .Bind().To<QueryService>()

            // Composition root
            .Root<QueryService>("QueryService");
}
// }