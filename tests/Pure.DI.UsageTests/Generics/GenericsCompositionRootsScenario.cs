/*
$v=true
$p=1
$d=Generic composition roots
$sa=Generic composition roots with constraints
$sa=Generic roots
$h=Sometimes you want to be able to create composition roots with type parameters. In this case, the composition root can only be represented by a method.
$h=>[!IMPORTANT]
$h=>`Resolve()` methods cannot be used to resolve generic composition roots.
$f=>[!IMPORTANT]
$f=>The method `Inject()` cannot be used outside of the binding setup.
$f=
$f=A command handler layer is the classic case: the same handler contract with one type parameter per command, one generated method per registration, and no reflection at run time. Tags separate alternative handlers for the same contract.
$f=A generic root is always a method, so it cannot be resolved through `Resolve` or exposed as a property. Code that needs a fixed type should use a non-generic root instead of closing the generic one by hand.
$f=>[!NOTE]
$f=>Every closed type used at a call site becomes its own generated method body. Wide generic roots over large graphs grow the generated file, so keep the graph behind the root focused.
$f=
$f=Prefer generic roots when the caller - not the composition - decides the type argument. See [Generic composition roots with constraints](generic-composition-roots-with-constraints.md) when the type parameter must satisfy a constraint.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedTypeParameter

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario;

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
            // Disable Resolve methods to keep the public API minimal
            .Hint(Hint.Resolve, "Off")
            .Bind().To<Repository<TT>>()
            .Bind().To<CreateCommandHandler<TT>>()
            // Creates UpdateCommandHandler manually,
            // just for the sake of example
            .Bind("Update").To(ctx => {
                ctx.Inject(out IRepository<TT> repository);
                return new UpdateCommandHandler<TT>(repository);
            })

            // Specifies to create a regular public method
            // to get a composition root of type ICommandHandler<T>
            // with the name "GetCreateCommandHandler"
            .Root<ICommandHandler<TT>>("GetCreateCommandHandler")

            // Specifies to create a regular public method
            // to get a composition root of type ICommandHandler<T>
            // with the name "GetUpdateCommandHandler"
            // using the "Update" tag
            .Root<ICommandHandler<TT>>("GetUpdateCommandHandler", "Update");

        var composition = new Composition();

        // createHandler = new CreateCommandHandler<int>(new Repository<int>());
        var createHandler = composition.GetCreateCommandHandler<int>();

        // updateHandler = new UpdateCommandHandler<string>(new Repository<string>());
        var updateHandler = composition.GetUpdateCommandHandler<string>();
// }
        createHandler.ShouldBeOfType<CreateCommandHandler<int>>();
        updateHandler.ShouldBeOfType<UpdateCommandHandler<string>>();
        composition.SaveClassDiagram();
    }
}

// {
interface IRepository<T>;

class Repository<T> : IRepository<T>;

interface ICommandHandler<T>;

class CreateCommandHandler<T>(IRepository<T> repository) : ICommandHandler<T>;

class UpdateCommandHandler<T>(IRepository<T> repository) : ICommandHandler<T>;
// }