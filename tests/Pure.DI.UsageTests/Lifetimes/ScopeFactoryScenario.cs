/*
$v=true
$p=4
$d=Scope factory
$h=Demonstrates scoped lifetime with `Hint(Hint.ScopeFactory, "on")` where scopes are represented by generated `Scope` objects created via `CreateScope()`.
$f=>[!NOTE]
$f=>This approach is useful when you need runtime scope creation without deriving a child composition type.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedMember.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Lifetimes.ScopeStateObjectScenario;

using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        var composition = new Composition(desc: "Checkout");
        IRequestContext ctx1;
        IRequestContext ctx2;

        // Request #1
        using (var request1 = composition.CreateScope())
        {
            var checkout11 = request1.RequestRoot;
            var checkout12 = request1.RequestRoot;
            ctx1 = checkout11.Context;

            // Same request => same scoped instance
            ctx1.ShouldBe(checkout12.Context);
            ctx1.IsDisposed.ShouldBeFalse();
        }

        // End of request #1 => scoped instance is disposed
        ctx1.IsDisposed.ShouldBeTrue();

        // Request #2
        using (var request2 = composition.CreateScope())
        {
            var checkout2 = request2.RequestRoot;
            ctx2 = checkout2.Context;
        }

        // Different request => different scoped instance
        ctx1.ShouldNotBe(ctx2);
        // End of request #2 => scoped instance is disposed
        ctx2.IsDisposed.ShouldBeTrue();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IIdGenerator
{
    Guid Generate();
}

class IdGenerator : IIdGenerator
{
    public Guid Generate() => Guid.NewGuid();
}

interface IRequestContext
{
    Guid CorrelationId { get; }

    bool IsDisposed { get; }
}

// Typically: DbContext / UnitOfWork / RequestTelemetry / Activity, etc.
sealed class RequestContext(IIdGenerator idGenerator)
    : IRequestContext, IDisposable
{
    public Guid CorrelationId { get; } = idGenerator.Generate();

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface ICheckoutService
{
    IRequestContext Context { get; }
}

// "Controller/service" that participates in request processing.
// It depends on a scoped context (per-request resource).
sealed class CheckoutService(string description, IRequestContext context) : ICheckoutService
{
    public IRequestContext Context => context;
}

partial class Composition
{
    static void Setup() =>
// }
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup()
            .Hint(Hint.ScopeFactoryName, "CreateScope")
            .Arg<string>("desc")
            // Per-request lifetime
            .Bind().As(Scoped).To<RequestContext>()

            .Bind().As(Singleton).To<IdGenerator>()

            // Regular service that consumes scoped context
            .Bind().To<CheckoutService>()

            // "Request root" (what your controller/handler resolves)
            .Root<ICheckoutService>("RequestRoot");
}
// }
