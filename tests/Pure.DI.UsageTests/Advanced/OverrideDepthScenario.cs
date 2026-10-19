/*
$v=true
$p=22
$d=Override depth
$sa=Overrides
$sa=Thread-safe overrides
$h=When this occurs: you need to control how far override values propagate in a factory.
$h=What it solves: keeps overrides local to a single injection level without affecting nested dependencies.
$h=How it is solved in the example: uses Let to keep overrides local and verifies the scope.
$h=`Override` applies to the whole subgraph resolved from the factory, while `Let` affects only the dependencies injected directly by it and does not propagate deeper.
$f=Use `Override` for a value that must reach every level - a correlation id, a tenant, a request context - and `Let` when one constructor argument must differ but its own dependencies should keep their normal bindings.
$f=Overrides are shared state of the resolution, so a factory that can run concurrently must wrap the override and the following `ctx.Inject` in `lock (ctx.Lock)` ([thread-safe overrides](thread-safe-overrides.md)).
$f=Deep overrides propagate into nested dependency graphs, including deferred factories created inside them; one-level overrides affect only the immediate injection.
$f=Prefer a tagged binding over an override when the different value is structural rather than per-call, because overrides live only for the duration of the factory invocation.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.OverrideDepthScenario;

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
        DI.Setup(nameof(DeepComposition))
            .Bind().To(_ => 7)
            .Bind().To<Dependency>()
            .Bind().To<Service>(ctx =>
            {
                ctx.Override(42);
                ctx.Inject(out Service service);
                return service;
            })
            .Root<Service>("Service");

        DI.Setup(nameof(ShallowComposition))
            .Bind().To(_ => 7)
            .Bind().To<Dependency>()
            .Bind().To<Service>(ctx =>
            {
                ctx.Let(42);
                ctx.Inject(out Service service);
                return service;
            })
            .Root<Service>("Service");

        var deep = new DeepComposition().Service;
        var shallow = new ShallowComposition().Service;

        deep.Id.ShouldBe(42);
        deep.Dependency.Id.ShouldBe(42);

        shallow.Id.ShouldBe(42);
        shallow.Dependency.Id.ShouldBe(7);
// }
        new DeepComposition().SaveClassDiagram();
        new ShallowComposition().SaveClassDiagram();
    }
}

// {
class Dependency(int id)
{
    public int Id { get; } = id;
}

class Service(int id, Dependency dependency)
{
    public int Id { get; } = id;

    public Dependency Dependency { get; } = dependency;
}
// }
