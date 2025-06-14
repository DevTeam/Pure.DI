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

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.ThreadsafeOverridesScenario;

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
                    ctx.Inject(Tag.Red, out Color red);

                    // Get composition sync root object
                    ctx.Inject(Tag.SyncRoot, out Lock lockObject);
                    lock (lockObject)
                    {
                        // Overrides with a lambda argument
                        ctx.Override(dependencyId);

                        // Overrides with tag using lambda argument
                        ctx.Override(subId, "sub");

                        // Overrides with some value
                        ctx.Override($"Dep {dependencyId} {subId}");

                        // Overrides with injected value
                        ctx.Override(red);

                        ctx.Inject<Dependency>(out var dependency);
                        return dependency;
                    }
                })
            .Bind().To<Service>()

            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependencies.Length.ShouldBe(100);
        for (var i = 0; i < 100; i++)
        {
            service.Dependencies.Count(dep => dep.Id == i).ShouldBe(1);
        }
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
            ..Enumerable.Range(0, 100).AsParallel().Select(i => dependencyFactory(i, 99))
        ];
}
// }