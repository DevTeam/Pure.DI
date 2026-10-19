/*
$v=true
$p=5
$d=Scope
$sa=Scoped
$sa=Scope setup method
$sa=Auto scoped
$h=The `Scoped` lifetime ensures a single instance of a dependency within a scope — a typical example is a single `DbContext`, unit of work, or request context per web request. This example wraps scope creation in a `Scope` class: each scope gets its own `RequestContext`, all services resolved within that scope share it, and disposing the scope disposes all scoped instances it created.
$h=The wrapper is what makes the boundary usable from application code: consumers ask for `NewScope`, use `Checkout` inside a `using` block, and never see the composition instance that implements the scope.
$f=>[!NOTE]
$f=>A scope is just another composition instance bound to its parent, so singletons remain shared across scopes while scoped instances are unique per scope.
$f=
$f=Use a wrapper like this when the boundary is a domain concept the rest of the code already has - a message handler, a test fixture, a background job iteration - and you do not want the composition type to leak into it.
$f=The wrapper owns the child composition, so it must dispose it: an undisposed scope keeps its scoped instances and everything they hold alive until the GC collects them. Concurrent scopes are independent, which is why this is the natural shape for parallel request handling.
$f=Prefer the generated `SetupScope` call from [Scope setup method](scope-setup-method.md) when no wrapper is needed, and [Auto scoped](auto-scoped.md) when the scope should be created behind a factory.
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
namespace Pure.DI.UsageTests.Lifetimes.ScopeScenario;

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
        var composition = new Composition();
        IRequestContext ctx1;
        IRequestContext ctx2;

        // Scope #1
        using (var scope1 = composition.NewScope)
        {
            var checkout11 = scope1.Checkout;
            var checkout12 = scope1.Checkout;
            ctx1 = checkout11.Context;

            // Same request => same scoped instance
            ctx1.ShouldBe(checkout12.Context);
            ctx1.IsDisposed.ShouldBeFalse();
        }

        // End of request #1 => scoped instance is disposed
        ctx1.IsDisposed.ShouldBeTrue();

        // Request #2
        using (var scope1 = composition.NewScope)
        {
            var checkout2 = scope1.Checkout;
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
sealed class CheckoutService(IRequestContext context)
    : ICheckoutService
{
    public IRequestContext Context => context;
}

// Represents a scope
class Scope(Composition composition): IDisposable
{
    private readonly Composition _scope = Composition.SetupScope(composition, new Composition());

    public ICheckoutService Checkout => _scope.RequestRoot;

    public void Dispose() => _scope.Dispose();
}

partial class Composition
{
    static void Setup() =>
// }
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup()
            .Hint(Hint.ScopeMethodName, "SetupScope")
            // Per-request lifetime
            .Bind().As(Scoped).To<RequestContext>()

            .Bind().As(Singleton).To<IdGenerator>()

            // Regular service that consumes scoped context
            .Bind().To<CheckoutService>()

            // "Request root" (what your controller/handler resolves)
            .Root<ICheckoutService>("RequestRoot")
            .Root<Scope>("NewScope");
}
// }
