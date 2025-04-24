/*
$v=true
$p=8
$d=Accumulators
$h=Accumulators allow you to accumulate instances of certain types and lifetimes.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.AccumulatorScenario;

using Shouldly;
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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Accumulate<IAccumulating, MyAccumulator>(Transient, Singleton)
            .Bind<IDependency>().As(PerBlock).To<AbcDependency>()
            .Bind<IDependency>(Tag.Type).To<AbcDependency>()
            .Bind<IDependency>(Tag.Type).As(Singleton).To<XyzDependency>()
            .Bind<IService>().To<Service>()
            .Root<(IService service, MyAccumulator accumulator)>("Root");

        var composition = new Composition();
        var (service, accumulator) = composition.Root;
        accumulator.Count.ShouldBe(3);
        accumulator[0].ShouldBeOfType<AbcDependency>();
        accumulator[1].ShouldBeOfType<XyzDependency>();
        accumulator[2].ShouldBeOfType<Service>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IAccumulating;

class MyAccumulator : List<IAccumulating>;

interface IDependency;

class AbcDependency : IDependency, IAccumulating;

class XyzDependency : IDependency, IAccumulating;

interface IService;

class Service(
    [Tag(typeof(AbcDependency))] IDependency dependency1,
    [Tag(typeof(XyzDependency))] IDependency dependency2,
    IDependency dependency3)
    : IService, IAccumulating;
// }