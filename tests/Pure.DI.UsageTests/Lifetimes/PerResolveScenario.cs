/*
$v=true
$p=2
$d=PerResolve
$sa=Transient
$sa=PerBlock
$sa=Singleton
$h=The `PerResolve` lifetime ensures that there will be one instance of the dependency for each composition root instance. Every access to a root starts a new resolution, so two accesses get two instances, while several injection sites inside one resolution get the same one.
$f=>[!NOTE]
$f=>`PerResolve` lifetime is useful when you want to share a dependency instance within a single composition root resolution.
$f=
$f=Reach for it when one operation needs a consistent collaborator - a pricing session, a request-scoped counter, a set of rules evaluated once - but that collaborator must not outlive the operation or be shared between operations.
$f=The scope is the root access itself, so a `PerResolve` instance is not disposed by the composition and is not visible to code that resolved the graph earlier. If the reuse boundary must be something the application can name and end, use `Scoped`; if it must live as long as the composition, use `Singleton`.
$f=>[!WARNING]
$f=>The scenario deliberately shows the common mistake: a `Singleton` tuple requests the `PerResolve` dependency, so it captures the instance created during the first resolution and keeps it forever. Inspect every singleton for `PerResolve`, `Scoped` and `Transient` dependencies before shipping.
$f=
$f=See [PerBlock](perblock.md) when reuse should stay inside one construction block instead of the whole root.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Lifetimes.PerResolveScenario;

using Xunit;
using Shouldly;
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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // PerResolve = one "planning session" per root access.
            // Imagine: each time you ask for a plan, you get a fresh context.
            .Bind().As(PerResolve).To<RoutePlanningSession>()

            // Singleton = created once per Composition instance.
            // Here it intentionally captures session when it's created the first time
            // (this is a realistic pitfall: singleton accidentally holds request-scoped state).
            .Bind().As(Singleton).To<(IRoutePlanningSession s3, IRoutePlanningSession s4)>()

            // Composition root
            .Root<TrainTripPlanner>("Planner");

        var composition = new Composition();

        // First "user request": plan a trip now
        var plan1 = composition.Planner;

        // In the same request, PerResolve dependencies are the same instance:
        plan1.SessionForOutbound.ShouldBe(plan1.SessionForReturn);

        // Tuple is Singleton, so both entries are the same captured instance:
        plan1.CapturedSessionA.ShouldBe(plan1.CapturedSessionB);

        // Because the singleton tuple was created during the first request,
        // it captured THAT request's PerResolve session:
        plan1.SessionForOutbound.ShouldBe(plan1.CapturedSessionA);

        // Second "user request": plan another trip (new root access)
        var plan2 = composition.Planner;

        // New request => new PerResolve session:
        plan2.SessionForOutbound.ShouldNotBe(plan1.SessionForOutbound);

        // But the singleton still holds the old captured session from the first request:
        plan2.CapturedSessionA.ShouldBe(plan1.CapturedSessionA);
        plan2.SessionForOutbound.ShouldNotBe(plan2.CapturedSessionA);
// }
        composition.SaveClassDiagram();
    }
}

// {
// A request-scoped context: e.g., contains "now", locale, pricing rules version,
// feature flags, etc. You typically want a new one per route planning request.
interface IRoutePlanningSession;

class RoutePlanningSession : IRoutePlanningSession;

// A service that plans a train trip.
// It asks for two session instances to demonstrate PerResolve:
// both should be the same within a single request.
class TrainTripPlanner(
    IRoutePlanningSession sessionForOutbound,
    IRoutePlanningSession sessionForReturn,
    (IRoutePlanningSession capturedA, IRoutePlanningSession capturedB) capturedSessions)
{
    public IRoutePlanningSession SessionForOutbound { get; } = sessionForOutbound;

    public IRoutePlanningSession SessionForReturn { get; } = sessionForReturn;

    // These come from a singleton tuple - effectively "global cached" instances.
    public IRoutePlanningSession CapturedSessionA { get; } = capturedSessions.capturedA;

    public IRoutePlanningSession CapturedSessionB { get; } = capturedSessions.capturedB;
}
// }