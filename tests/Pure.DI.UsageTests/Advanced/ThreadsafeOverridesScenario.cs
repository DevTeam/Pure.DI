/*
$v=true
$p=11
$d=Thread-safe overrides
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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
        // FormatCode = On
// {    
        DI.Setup(nameof(Composition))
            .Bind("Global").To(_ => new ProcessingToken("TOKEN-123"))
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