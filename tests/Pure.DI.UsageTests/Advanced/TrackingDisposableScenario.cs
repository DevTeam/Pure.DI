/*
$v=true
$p=100
$d=Tracking disposable instances per a composition root
$sa=Tracking disposable instances in delegates
$sa=Tracking disposable instances with different lifetimes
$sa=Disposable singleton
$h=The special `Owned<T>` type lets you track and dispose of disposable instances per composition root rather than per composition. Declare a root as `Root<Owned<T>>`: each access returns an `Owned<T>` that owns every disposable created for that dependency graph, and calling its `Dispose()` cleans up exactly those instances without affecting other roots.
$f=>[!NOTE]
$f=>Disposable tracking ensures proper cleanup of all disposable instances within a composition scope.
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers

namespace Pure.DI.UsageTests.Advanced.TrackingDisposableScenario;

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
        var orderProcessingService1 = composition.OrderProcessingService;
        var orderProcessingService2 = composition.OrderProcessingService;

        orderProcessingService2.Dispose();

        // Checks that the disposable instances
        // associated with orderProcessingService2 have been disposed of
        orderProcessingService2.Value.DbConnection.IsDisposed.ShouldBeTrue();

        // Checks that the disposable instances
        // associated with orderProcessingService1 have not been disposed of
        orderProcessingService1.Value.DbConnection.IsDisposed.ShouldBeFalse();

        orderProcessingService1.Dispose();

        // Checks that the disposable instances
        // associated with orderProcessingService1 have been disposed of
        orderProcessingService1.Value.DbConnection.IsDisposed.ShouldBeTrue();
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

interface IOrderProcessingService
{
    public IDbConnection DbConnection { get; }
}

class OrderProcessingService(IDbConnection dbConnection) : IOrderProcessingService
{
    public IDbConnection DbConnection { get; } = dbConnection;
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
            .Bind().To<OrderProcessingService>()

            // A special composition root
            // that allows to manage disposable dependencies
            .Root<Owned<IOrderProcessingService>>("OrderProcessingService");
}
// }