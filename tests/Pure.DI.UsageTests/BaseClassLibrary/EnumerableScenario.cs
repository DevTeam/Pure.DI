/*
$v=true
$p=1
$d=Enumerable
$h=Specifying `IEnumerable<T>` as the injection type allows you to inject instances of all bindings that implement type `T` in a lazy fashion - the instances will be provided one by one, in order corresponding to the sequence of bindings.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.BCL.EnumerableScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
interface IDependency { }

class AbcDependency : IDependency { }

class XyzDependency : IDependency { }

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service : IService
{
    public Service(IEnumerable<IDependency> dependencies) => 
        Dependencies = dependencies.ToImmutableArray();

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
            .Bind<IDependency>().To<AbcDependency>()
            .Bind<IDependency>(2).To<XyzDependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependencies.Length.ShouldBe(2);
        service.Dependencies[0].ShouldBeOfType<AbcDependency>();
        service.Dependencies[1].ShouldBeOfType<XyzDependency>();
// }            
        TestTools.SaveClassDiagram(new Composition(), nameof(EnumerableScenario));
    }
}