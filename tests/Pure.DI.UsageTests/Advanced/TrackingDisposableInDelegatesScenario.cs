/*
$v=true
$p=101
$d=Tracking disposable instances in delegates
$h=When a service creates disposable dependencies dynamically, inject `Func<Owned<T>>` instead of `Func<T>`. Each factory call returns an `Owned<T>` that owns all disposables created for that particular graph, so disposing it cleans up exactly those instances — graphs produced by other factory calls remain alive.
$f=>[!NOTE]
$f=>Disposable tracking in delegates ensures proper cleanup even when instances are created dynamically through factory delegates.
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable UnusedMemberInSuper.Global

namespace Pure.DI.UsageTests.Advanced.TrackingDisposableInDelegatesScenario;

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
        var transaction1 = composition.Transaction;
        var transaction2 = composition.Transaction;

        transaction2.Dispose();

        // Checks that the disposable instances
        // associated with transaction2 have been disposed of
        transaction2.Connection.IsDisposed.ShouldBeTrue();

        // Checks that the disposable instances
        // associated with transaction1 have not been disposed of
        transaction1.Connection.IsDisposed.ShouldBeFalse();

        transaction1.Dispose();

        // Checks that the disposable instances
        // associated with transaction1 have been disposed of
        transaction1.Connection.IsDisposed.ShouldBeTrue();
// }
        new Composition().SaveClassDiagram();
    }
}

// {
interface IDbConnection
{
    bool IsDisposed { get; }
}

class DbConnection : IDbConnection, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface ITransaction
{
    IDbConnection Connection { get; }
}

class Transaction(Func<Owned<IDbConnection>> connectionFactory)
    : ITransaction, IDisposable
{
    private readonly Owned<IDbConnection> _connection = connectionFactory();

    public IDbConnection Connection => _connection.Value;

    public void Dispose() => _connection.Dispose();
}

partial class Composition
{
    static void Setup() =>
// }
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup()
            .Bind().To<DbConnection>()
            .Bind().To<Transaction>()

            // Composition root
            .Root<Transaction>("Transaction");
}
// }