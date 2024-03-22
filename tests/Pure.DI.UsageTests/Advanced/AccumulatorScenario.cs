/*
$v=true
$p=8
$d=Accumulators
$h=Accumulators allow you to accumulate instances of certain types and lifetimes.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.UsageTests.Advanced.AccumulatorScenario;

using Shouldly;
using Xunit;

// {
interface IAccumulating { }

class MyAccumulator: List<IAccumulating>;

interface IDependency;

class AbcDependency : IDependency, IAccumulating;
        
class XyzDependency : IDependency;
        
class Dependency : IDependency;

interface IService: IAccumulating
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }
    
    IDependency Dependency3 { get; }
}

class Service(
    [Tag(typeof(AbcDependency))] IDependency dependency1,
    [Tag(typeof(XyzDependency))] IDependency dependency2,
    IDependency dependency3)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public IDependency Dependency3 { get; } = dependency3;
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup(nameof(Composition))
            .Accumulate<IAccumulating, MyAccumulator>(Lifetime.Transient, Lifetime.Singleton)
            .Bind<IDependency>().As(Lifetime.PerBlock).To<AbcDependency>()
            .Bind<IDependency>(Tag.Type).To<AbcDependency>()
            .Bind<IDependency>(Tag.Type).As(Lifetime.Singleton).To<XyzDependency>()
            .Bind<IService>().To<Service>()
            .Root<(IService service, MyAccumulator accumulator)>("Root");

        var composition = new Composition();
        var root = composition.Root;
        var service = root.service;
        var accumulator = root.accumulator;
        accumulator.Count.ShouldBe(3);
        accumulator[0].ShouldBeOfType<AbcDependency>();
        accumulator[1].ShouldBeOfType<AbcDependency>();
        accumulator[2].ShouldBeOfType<Service>();
        
// }            
        composition.SaveClassDiagram();
    }
}