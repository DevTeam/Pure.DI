/*
$v=true
$p=9
$d=Generic builder
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI.UsageTests.Generics.GenericBuilderScenario;

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
            .Builder<Service<TTS, TT2>>("BuildUpGeneric");

        var composition = new Composition();
        var service = composition.BuildUpGeneric(new Service<Guid, string>());
        service.Id.ShouldNotBe(Guid.Empty);
        service.Dependency.ShouldBeOfType<Dependency<string>>();
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

record Service<T, T2>: IService<T, T2>
    where T: struct
{
    public T Id { get; private set; }
    
    [Dependency]
    public IDependency<T2>? Dependency { get; set; }

    [Dependency]
    public void SetId([Tag(Tag.Id)] T id) => Id = id;
}
// }