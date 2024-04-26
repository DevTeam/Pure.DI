/*
$v=true
$p=1
$d=Enumerable generics
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedTypeParameter
namespace Pure.DI.UsageTests.BCL.EnumerableGenericsScenario;

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
        = [..dependencies];
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup(nameof(Composition))
            .Bind<IDependency<TT>>().To<AbcDependency<TT>>()
            .Bind<IDependency<TT>>("Xyz").To<XyzDependency<TT>>()
            .Bind<IService<TT>>().To<Service<TT>>()
            
            // Composition roots
            .Root<IService<int>>("IntRoot")
            .Root<IService<string>>("StringRoot");

        var composition = new Composition();
        
        var intService = composition.IntRoot;
        intService.Dependencies.Length.ShouldBe(2);
        intService.Dependencies[0].ShouldBeOfType<AbcDependency<int>>();
        intService.Dependencies[1].ShouldBeOfType<XyzDependency<int>>();
        
        var stringService = composition.StringRoot;
        stringService.Dependencies.Length.ShouldBe(2);
        stringService.Dependencies[0].ShouldBeOfType<AbcDependency<string>>();
        stringService.Dependencies[1].ShouldBeOfType<XyzDependency<string>>();
// }            
        composition.SaveClassDiagram();
    }
}