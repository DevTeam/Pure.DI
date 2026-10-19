/*
$v=true
$t=Advanced
$t=HighPerformance
$p=21
$p=HighPerformance:16
$d=Thread-safe overrides
$sa=Overrides
$sa=Override depth
$h=When a factory delegate can be invoked from several threads at once — as with the `Func<int, int, IOrderHandler>` called in parallel here — its `ctx.Override(...)` calls must be synchronized. Wrap the overrides together with the subsequent `ctx.Inject(...)` in a `lock (ctx.Lock)` block so that each object graph is built with its own override values and parallel invocations don't overwrite each other.
$f=This is the shape of a batch job that turns a range of inputs into handlers in parallel: each call must see its own values, and the shared services - the time provider, the token - must still be injected once.
$f=The lock must cover the overrides and the `ctx.Inject` that consumes them, because overrides are shared state of the resolution; splitting them lets another thread mix its values into this graph.
$f=A value that is the same for every call does not need an override: inject it with `ctx.Inject` before the lock, as the processing token is injected here.
$f=Without the lock the graph usually still compiles and may pass a single-threaded test, then fail under load with values from another call - the failure appears in the data, not in an exception.
$f=The same rule applies to stack-only values such as `Span<T>`, `ReadOnlySpan<T>`, and generic `T` with `where T : allows ref struct`. Pure.DI reports `DIW013` when such values are overridden in a factory delegate without `lock (ctx.Lock)` while thread safety is enabled.
$f=>[!IMPORTANT]
$f=>Thread-safe overrides are essential when composition instances are shared across multiple threads or when parallel resolution is required.
$f=
$f=Prefer `Func<...>` factory bindings with arguments when you can, because the generated delegate synchronizes the overrides for you.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable VariableHidesOuterVariable
// ReSharper disable NotAccessedPositionalProperty.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.ThreadsafeOverridesScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Immutable;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
        // FormatCode = On
// {    
        DI.Setup(nameof(Composition))
            .Bind("Global").To(() => new ProcessingToken("TOKEN-123"))
            .Bind().As(Lifetime.Singleton).To<TimeProvider>()
            .Bind().To<Func<int, int, IOrderHandler>>(ctx =>
                (orderId, customerId) => {
                    // Retrieves a global processing token to be passed to the handler
                    ctx.Inject("Global", out ProcessingToken token);

                    // The factory is invoked in parallel, so we must lock
                    // the context to safely perform overrides for the specific graph
                    lock (ctx.Lock)
                    {
                        // Overrides the 'int' dependency (OrderId)
                        ctx.Override(orderId);

                        // Overrides the tagged 'int' dependency (CustomerId)
                        ctx.Override(customerId, "customer");

                        // Overrides the 'string' dependency (TraceId)
                        ctx.Override($"Order:{orderId}-Cust:{customerId}");

                        // Overrides the 'ProcessingToken' dependency with the injected value
                        ctx.Override(token);

                        // Creates the handler with the overridden dependencies
                        ctx.Inject<OrderHandler>(out var handler);
                        return handler;
                    }
                })
            .Bind().To<OrderBatchProcessor>()

            // Composition root
            .Root<IOrderBatchProcessor>("OrderProcessor");

        var composition = new Composition();
        var orderProcessor = composition.OrderProcessor;

        orderProcessor.Handlers.Length.ShouldBe(100);
        for (var i = 0; i < 100; i++)
        {
            orderProcessor.Handlers.Count(h => h.OrderId == i).ShouldBe(1);
        }
// }
        composition.SaveClassDiagram();
    }
}

// {
record ProcessingToken(string Value);

interface ITimeProvider
{
    DateTimeOffset Now { get; }
}

class TimeProvider : ITimeProvider
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IOrderHandler
{
    string TraceId { get; }

    int OrderId { get; }

    int CustomerId { get; }
}

class OrderHandler(
    string traceId,
    ITimeProvider timeProvider,
    int orderId,
    [Tag("customer")] int customerId,
    ProcessingToken token)
    : IOrderHandler
{
    public string TraceId => traceId;

    public int OrderId => orderId;

    public int CustomerId => customerId;
}

interface IOrderBatchProcessor
{
    ImmutableArray<IOrderHandler> Handlers { get; }
}

class OrderBatchProcessor(Func<int, int, IOrderHandler> orderHandlerFactory)
    : IOrderBatchProcessor
{
    public ImmutableArray<IOrderHandler> Handlers { get; } =
    [
        // Simulates parallel processing of orders
        ..Enumerable.Range(0, 100)
            .AsParallel()
            .Select(i => orderHandlerFactory(i, 99))
    ];
}
// }
