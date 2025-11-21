/*
$v=true
$p=2
$d=PerResolve
$h=The _PerResolve_ lifetime ensures that there will be one instance of the dependency for each composition root instance.
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
        // This hint indicates to not generate methods such as Resolve
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

    // These come from a singleton tuple — effectively "global cached" instances.
    public IRoutePlanningSession CapturedSessionA { get; } = capturedSessions.capturedA;

    public IRoutePlanningSession CapturedSessionB { get; } = capturedSessions.capturedB;
}
// }