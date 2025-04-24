/*
$v=true
$p=4
$d=Tag Unique
$h=`Tag.Unique` is useful to register a binding with a unique tag. It will not be available through the composition root or `Resolve` methods directly, but can be injected in compositions as some kind of enumeration.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedTypeParameter

namespace Pure.DI.UsageTests.Advanced.TagUniqueScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Immutable;
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
            .Bind<IDependency<TT>>(Tag.Unique).To<AbcDependency<TT>>()
            .Bind<IDependency<TT>>(Tag.Unique).To<XyzDependency<TT>>()
            .Bind<IService<TT>>().To<Service<TT>>()

            // Composition root
            .Root<IService<string>>("Root");

        var composition = new Composition();
        var stringService = composition.Root;
        stringService.Dependencies.Length.ShouldBe(2);
// }
        composition.SaveClassDiagram();
    }
}

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