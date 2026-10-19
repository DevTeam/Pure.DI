/*
$v=true
$p=12
$d=Async disposable singleton
$sa=Disposable singleton
$sa=Async disposable scope
$h=If at least one of these objects implements the `IAsyncDisposable` interface, then the composition implements `IAsyncDisposable` as well. To dispose of all created singleton instances in an asynchronous manner, simply dispose of the composition instance in an asynchronous manner. The composition keeps both contracts: a synchronous `Dispose` still works, but it blocks on each asynchronous release.
$f=>[!NOTE]
$f=>Async disposable singleton ensures proper async cleanup of all singleton resources when the composition is disposed.
$f=
$f=Audit writers, message producers, flush-on-exit telemetry and connection pools are the usual owners of this contract: their release performs real I/O and must not block a shutdown thread.
$f=>[!IMPORTANT]
$f=>Use `await using` or `await DisposeAsync()`. Calling `Dispose()` only blocks: for an async-only resource the generated code waits on `DisposeAsync().GetAwaiter().GetResult()`, which inside a synchronization context can deadlock and on a shutdown thread turns every async release into a blocking call.
$f=
$f=Disposal order is reverse to creation and each failure is reported separately through the `OnDisposeException` or `OnDisposeAsyncException` partial method, so one broken resource does not prevent the others from being released. Override them to log, otherwise the failure is silent.
$f=Prefer this whenever a singleton holds I/O; when the resource closes synchronously, [Disposable singleton](disposable-singleton.md) is enough and simpler.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Global
namespace Pure.DI.UsageTests.Lifetimes.AsyncDisposableSingletonScenario;

using Shouldly;
using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
//# using System.Threading.Tasks;
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // A singleton resource that needs async cleanup (e.g., flushing buffers, closing connections)
            .Bind().As(Singleton).To<AuditLogWriter>()
            .Bind().To<CheckoutService>()
            .Root<ICheckoutService>("CheckoutService");

        AuditLogWriter writer;

        await using (var composition = new Composition())
        {
            var service = composition.CheckoutService;

            // A "live" usage: do some work that writes to an audit log
            await service.CheckoutAsync(orderId: "ORD-2025-00042");

            // Keep a reference so we can assert disposal after the composition is disposed
            writer = service.Writer;
            writer.IsDisposed.ShouldBeFalse();
        }

        // Composition disposal triggers async disposal of singleton(s)
        writer.IsDisposed.ShouldBeTrue();
// }
        new Composition().SaveClassDiagram();
    }
}

// {
interface ICheckoutService
{
    AuditLogWriter Writer { get; }

    ValueTask CheckoutAsync(string orderId);
}

/// <summary>
/// Represents a singleton infrastructure component.
/// Think: audit log writer, message producer, telemetry pipeline, DB connection, etc.
/// It is owned by the DI composition and must be disposed asynchronously.
/// </summary>
sealed class AuditLogWriter : IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public async ValueTask WriteAsync(string message)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, nameof(AuditLogWriter));
        // Simulate I/O (writing to file / network / remote log)
        await Task.Delay(5);
    }

    public async ValueTask DisposeAsync()
    {
        // Simulate async cleanup: flush buffers / send remaining events / gracefully close connection
        await Task.Delay(5);
        IsDisposed = true;
    }
}

sealed class CheckoutService(AuditLogWriter writer) : ICheckoutService
{
    public AuditLogWriter Writer { get; } = writer;

    public ValueTask CheckoutAsync(string orderId)
    {
        // Real-world-ish side effect: record a business event
        return Writer.WriteAsync($"Checkout completed: {orderId}");
    }
}
// }