/*
$v=true
$p=11
$d=Generic builder
$sa=Builder
$sa=Generic builders
$h=Builders can be generic as well. `Builder<ViewModel<TTS, TT2>>("BuildUp")` generates a generic `BuildUp` method that injects dependencies into an instance you already have — handy when objects are created by an external framework (a UI library, a serializer) rather than by the composition.
$h=The marker types define the method's type parameters: `TTS` matches the `struct` constraint on `TId`, and `TT2` stands for the model type.
$f=>[!NOTE]
$f=>Generic builders enable flexible object initialization while maintaining type safety across different generic types.
$f=
$f=This is the shape used when an external framework owns creation - a UI view model instantiated by a page, a message materialized by a serializer, an entity loaded by an ORM - and only the dependencies can be supplied afterwards. The build-up method is generated for the exact closed types you pass to it.
$f=Member and method injection run in a fixed order defined by the builder, and a member with no matching binding stays at its default value instead of failing, so verify the properties you rely on.
$f=>[!NOTE]
$f=>`BuildUp` mutates the instance and returns it, which means the injected dependencies are not tracked by the composition: disposal stays with whoever owns the object.
$f=
$f=Prefer constructor injection for objects the composition creates itself, and builders only for objects born outside it. See [Generic builders](generic-builders.md) when several implementations need the same treatment.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ClassNeverInstantiated.Global
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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind(Tag.Id).To(() => (TT)(object)Guid.NewGuid())
            .Bind().To<Repository<TT>>()
            // Generic service builder
            // Defines a generic builder "BuildUp".
            // This is useful when instances are created by an external framework
            // (like a UI library or serialization) but require dependencies to be injected.
            .Builder<ViewModel<TTS, TT2>>("BuildUp");

        var composition = new Composition();

        // A view model instance created manually (or by a UI framework)
        var viewModel = new ViewModel<Guid, Customer>();

        // Inject dependencies (Id and Repository) into the existing instance
        var builtViewModel = composition.BuildUp(viewModel);

        builtViewModel.Id.ShouldNotBe(Guid.Empty);
        builtViewModel.Repository.ShouldBeOfType<Repository<Customer>>();
// }
        composition.SaveClassDiagram();
    }
}

// {
// Domain model
record Customer;

interface IRepository<T>;

class Repository<T> : IRepository<T>;

interface IViewModel<out TId, TModel>
{
    TId Id { get; }

    IRepository<TModel>? Repository { get; }
}

// The view model is generic, allowing it to be used for various entities
record ViewModel<TId, TModel> : IViewModel<TId, TModel>
    where TId : struct
{
    public TId Id { get; private set; }

    // The dependency to be injected
    [Dependency]
    public IRepository<TModel>? Repository { get; set; }

    // Method injection for the ID
    [Dependency]
    public void SetId([Tag(Tag.Id)] TId id) => Id = id;
}
// }