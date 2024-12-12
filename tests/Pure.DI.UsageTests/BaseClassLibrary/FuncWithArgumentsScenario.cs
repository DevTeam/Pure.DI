/*
$v=true
$p=99
$d=Func with arguments
$f=Using a binding of the form `.Bind<T>().To<T>("some statement")` is a kind of hack that allows you to replace an injection with just its own string.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable VariableHidesOuterVariable

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

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
    int Id { get; }

    int SubId { get; }
}

class Dependency(
    IClock clock,
    int id,
    [Tag("sub")] int subId)
    : IDependency
{
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

public class Scenario
{
    [Fact]
    public void Run()
    {
        // FormatCode = On
// {    
        DI.Setup(nameof(Composition))
            .Bind<IClock>().As(Lifetime.Singleton).To<Clock>()
            // Binds a dependency of type int
            // to the source code statement "dependencyId"
            .Bind<int>().To<int>("dependencyId")
            // Binds a dependency of type int with tag "sub"
            // to the source code statement "subId"
            .Bind<int>("sub").To<int>("subId")
            .Bind<Func<int, int, IDependency>>()
            .To<Func<int, int, IDependency>>(ctx =>
                (dependencyId, subId) =>
                {
                    // Builds up an instance of type Dependency
                    // referring source code statements "dependencyId"
                    // and source code statements "subId"
                    ctx.Inject<Dependency>(out var dependency);
                    return dependency;
                })
            .Bind<IService>().To<Service>()

            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependencies.Length.ShouldBe(3);
        service.Dependencies[0].Id.ShouldBe(0);
        service.Dependencies[1].Id.ShouldBe(1);
        service.Dependencies[2].Id.ShouldBe(2);
// }
        composition.SaveClassDiagram();
    }
}