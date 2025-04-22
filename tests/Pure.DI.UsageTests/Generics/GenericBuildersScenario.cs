/*
$v=true
$p=9
$d=Generic builders
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnassignedGetOnlyAutoProperty
namespace Pure.DI.UsageTests.Generics.GenericBuildersScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
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
            .Bind(Tag.Id).To<TT>(_ => (TT)(object)Guid.NewGuid())
            .Bind().To<Dependency<TT>>()
            // Generic service builder
            .Builders<IService<TT, TT2>>("BuildUpGeneric");

        var composition = new Composition();

        var service1 = composition.BuildUpGeneric(new Service1<Guid, string>());
        service1.Id.ShouldNotBe(Guid.Empty);
        service1.Dependency.ShouldBeOfType<Dependency<string>>();

        var service2 = composition.BuildUpGeneric(new Service2<Guid, int>());
        service2.Id.ShouldBe(Guid.Empty);
        service2.Dependency.ShouldBeOfType<Dependency<int>>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<out T, T2>
{
    T Id { get; }
    
    IDependency<T2>? Dependency { get; }
}

record Service1<T, T2>: IService<T, T2>
    where T: struct
{
    public T Id { get; private set; }
    
    [Dependency]
    public IDependency<T2>? Dependency { get; set; }

    [Dependency]
    public void SetId([Tag(Tag.Id)] T id) => Id = id;
}

record Service2<T, T2>: IService<T, T2>
    where T: struct
{
    public T Id { get; }

    [Dependency]
    public IDependency<T2>? Dependency { get; set; }
}
// }