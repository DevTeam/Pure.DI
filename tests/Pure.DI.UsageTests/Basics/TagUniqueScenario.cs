/*
$v=true
$p=6
$d=Tag Unique
$h=`Tag.Unique` is useful to register a binding with a unique tag. It will not be available through the composition root or `Resolve` methods directly, but can be embedded in compositions as some kind of enumeration.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedTypeParameter
namespace Pure.DI.UsageTests.Basics.TagUniqueScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
interface IDependency<T>;

class AbcDependency<T> : IDependency<T>;

class XyzDependency<T> : IDependency<T>;

interface IService<T>
{
    ImmutableArray<IDependency<T>> Dependencies { get; }
}

class Service<T>(IEnumerable<IDependency<T>> dependencies) : IService<T>
{
    public ImmutableArray<IDependency<T>> Dependencies { get; }
        = dependencies.ToImmutableArray();
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup(nameof(Composition))
            .Bind<IDependency<TT>>(Tag.Unique).To<AbcDependency<TT>>()
            .Bind<IDependency<TT>>(Tag.Unique).To<XyzDependency<TT>>()
            .Bind<IService<TT>>().To<Service<TT>>()
                .Root<IService<string>>("Root");

        var composition = new Composition();
        var stringService = composition.Root;
        stringService.Dependencies.Length.ShouldBe(2);
// }            
        composition.SaveClassDiagram();
    }
}