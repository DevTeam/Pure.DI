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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind(Tag.Id).To<TT>(_ => (TT)(object)Guid.NewGuid())
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