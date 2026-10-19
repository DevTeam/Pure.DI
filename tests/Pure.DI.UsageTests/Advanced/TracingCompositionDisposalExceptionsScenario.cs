/*
$v=true
$p=106
$d=Tracing exceptions during composition disposal
$sa=Tracing exceptions during Owned disposal
$sa=Tracking disposable instances per a composition root
$h=A generated composition catches exceptions thrown while disposing tracked singleton and scoped dependencies, invokes the `OnDisposeException` partial method, and continues disposing the remaining resources. Implement this method in the composition partial class to send failures to logging, tracing, or monitoring without wrapping every resource manually.
$f=This is what you want at application shutdown: one failing client must not prevent the rest of the singletons - database connections, pools, writers - from being released.
$f=The hook receives the concrete instance and the exception, so it can attach the type name, the resource identity and the message to the log or the telemetry of the host.
$f=Disposal continues in reverse creation order after the hook returns; without an implementation the exception is still suppressed, so a composition silently continues unless you implement the hook.
$f=The partial method exists only when the composition has something to dispose, and an async counterpart (`OnDisposeAsyncException`) is generated when the graph contains `IAsyncDisposable` resources.
$f=>[!IMPORTANT]
$f=>The exception is suppressed after `OnDisposeException` returns so that cleanup can continue. If the application must fail shutdown, the partial method can record the failure and then throw according to the application's shutdown policy.
$f=
$f=Prefer `OnDispose` ([custom dispose strategy](custom-dispose-strategy.md)) when the shutdown itself must be different, not just observed.
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers

namespace Pure.DI.UsageTests.Advanced.TracingCompositionDisposalExceptionsScenario;

using Shouldly;
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
        var orderProcessor = composition.OrderProcessor;

        // Simulates application shutdown. The payment gateway throws while
        // closing, but the database connection must still be released.
        composition.Dispose();

        composition.Events.ShouldBe([
            "PaymentGatewayConnection: The remote payment session did not close cleanly."]);
        orderProcessor.Database.IsDisposed.ShouldBeTrue();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IOrderDatabase
{
    bool IsDisposed { get; }
}

// Represents a long-lived database connection owned by the application.
class OrderDatabase : IOrderDatabase, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IPaymentGatewayConnection;

// Represents an external client that can fail during application shutdown.
class PaymentGatewayConnection : IPaymentGatewayConnection, IDisposable
{
    public void Dispose() =>
        throw new IOException("The remote payment session did not close cleanly.");
}

interface IOrderProcessor
{
    IOrderDatabase Database { get; }
}

class OrderProcessor(
    IOrderDatabase database,
    IPaymentGatewayConnection paymentGatewayConnection)
    : IOrderProcessor
{
    public IOrderDatabase Database { get; } = database;

    public IPaymentGatewayConnection PaymentGatewayConnection { get; } = paymentGatewayConnection;
}

partial class Composition
{
    static void Setup() =>
// }
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup()
            .Bind<IOrderDatabase>().As(Lifetime.Singleton).To<OrderDatabase>()
            .Bind<IPaymentGatewayConnection>().As(Lifetime.Singleton).To<PaymentGatewayConnection>()
            .Bind<IOrderProcessor>().To<OrderProcessor>()
            .Root<IOrderProcessor>("OrderProcessor");

    private readonly List<string> _events = [];

    // Called whenever a tracked IDisposable throws during composition disposal.
    partial void OnDisposeException<T>(T disposableInstance, Exception exception)
        where T : IDisposable =>
        _events.Add($"{disposableInstance.GetType().Name}: {exception.Message}");

    public IReadOnlyList<string> Events => _events;

    public void Clear() => _events.Clear();
}
// }
