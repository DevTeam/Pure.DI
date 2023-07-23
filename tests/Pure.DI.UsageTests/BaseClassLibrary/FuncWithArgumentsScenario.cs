﻿/*
$v=true
$p=99
$d=Func with arguments
$h=At any time a BCL type binding can be added manually:
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Global
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
}

class Dependency : IDependency
{
    public Dependency(IClock clock) { }

    public int Id { get; set; }
}

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service : IService
{
    public Service(Func<int, IDependency> dependencyFactory) =>
        Dependencies = Enumerable
            .Range(0, 10)
            .Select((_, index) => dependencyFactory(index))
            .ToImmutableArray();

    public ImmutableArray<IDependency> Dependencies { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
        // FormatCode = On
// {            
        DI.Setup("Composition")
            .Bind<IClock>().As(Lifetime.Singleton).To<Clock>()
            .Bind<Func<int, IDependency>>().To(ctx =>
                new Func<int, IDependency>(id =>
                {
                    ctx.Inject<Dependency>(out var dependency);
                    dependency.Id = id;
                    return dependency;
                }))
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependencies.Length.ShouldBe(10);
        service.Dependencies[3].Id.ShouldBe(3);
// }            
        TestTools.SaveClassDiagram(new Composition(), nameof(FuncWithArgumentsScenario));
    }
}