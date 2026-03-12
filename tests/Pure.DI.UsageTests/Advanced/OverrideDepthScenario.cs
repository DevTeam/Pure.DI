/*
$v=true
$p=12
$d=Override depth
$h=When this occurs: you need to control how far override values propagate in a factory.
$h=What it solves: keeps overrides local to a single injection level without affecting nested dependencies.
$h=How it is solved in the example: uses Let to keep overrides local and verifies the scope.
$f=
$f=What it shows:
$f=- Demonstrates deep vs one-level override behavior.
$f=
$f=Important points:
$f=- Deep overrides propagate into nested dependency graphs.
$f=- One-level overrides affect only the immediate injection.
$f=
$f=Useful when:
$f=- You want to override a constructor parameter without affecting deeper object graphs.
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
