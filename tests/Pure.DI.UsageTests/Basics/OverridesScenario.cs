/*
$v=true
$p=16
$d=Overrides
$h=This example demonstrates advanced dependency injection techniques using Pure.DI's override mechanism to customize dependency instantiation with runtime arguments and tagged parameters. The implementation creates multiple `IDependency` instances with values manipulated through explicit overrides.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable VariableHidesOuterVariable

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.OverridesScenario;

using System.Collections.Immutable;
using System.Drawing;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Immutable;
//# using System.Drawing;
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
            .Bind(Tag.Red).To(_ => Color.Red)
            .Bind().As(Lifetime.Singleton).To<Clock>()
            .Bind().To<Func<int, int, IDependency>>(ctx =>
                (dependencyId, subId) =>
                {
                    // Overrides with a lambda argument
                    ctx.Override(dependencyId);

                    // Overrides with tag using lambda argument
                    ctx.Override(subId, "sub");

                    // Overrides with some value
                    ctx.Override($"Dep {dependencyId} {subId}");

                    // Overrides with injected value
                    ctx.Inject(Tag.Red, out Color red);
                    ctx.Override(red);

                    ctx.Inject<Dependency>(out var dependency);
                    return dependency;
                })
            .Bind().To<Service>()

            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependencies.Length.ShouldBe(3);

        service.Dependencies[0].Id.ShouldBe(0);
        service.Dependencies[0].SubId.ShouldBe(99);
        service.Dependencies[0].Name.ShouldBe("Dep 0 99");

        service.Dependencies[1].Id.ShouldBe(1);
        service.Dependencies[1].Name.ShouldBe("Dep 1 99");

        service.Dependencies[2].Id.ShouldBe(2);
        service.Dependencies[2].Name.ShouldBe("Dep 2 99");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IClock
{
    DateTimeOffset Now { get; }
}

class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IDependency
{
    string Name { get; }

    int Id { get; }

    int SubId { get; }
}

class Dependency(
    string name,
    IClock clock,
    int id,
    [Tag("sub")] int subId,
    Color red)
    : IDependency
{
    public string Name => name;

    public int Id => id;

    public int SubId => subId;
}

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service(Func<int, int, IDependency> dependencyFactory): IService
{
    public ImmutableArray<IDependency> Dependencies { get; } =
    [
        dependencyFactory(0, 99),
        dependencyFactory(1, 99),
        dependencyFactory(2, 99)
    ];
}
// }