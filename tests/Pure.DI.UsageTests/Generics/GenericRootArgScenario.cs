/*
$v=true
$p=6
$d=Generic root arguments
$sa=Root arguments
$sa=Complex generic root arguments
$h=Sometimes a composition root needs an argument whose type depends on the root's own type parameter. Declaring `RootArg<TT>("model")` together with the generic root `Root<IPresenter<TT>>("GetPresenter")` produces a generic method `GetPresenter<T>(T model)`.
$h=The value passed to that method is injected into `Presenter<T>` through the method marked with the `[Dependency]` attribute.
$f=>[!NOTE]
$f=>Generic root arguments enable flexible type parameterization while maintaining compile-time type safety.
$f=
$f=Typical use is a presenter, a serializer or a handler whose payload type follows the root's own type argument: `GetPresenter<Invoice>(invoice)` builds a fully wired presenter for the object the caller already has.
$f=The argument becomes part of the generated method signature, so the composition stays free of the request data and no placeholder binding is needed. Resolution still happens at compile time.
$f=>[!WARNING]
$f=>Root arguments are consumed once per resolution and are not shared with dependencies created later. A dependency that needs the same value must receive it through the same argument, not through a captured closure.
$f=
$f=Prefer a root argument for per-call data that belongs to the root, and constructor parameters for fixed configuration. See [Complex generic root arguments](complex-generic-root-arguments.md) when the argument type is generic itself.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable UnusedVariable
// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI.UsageTests.Generics.GenericRootArgScenario;

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
            .RootArg<TT>("model")
            .Bind<IPresenter<TT>>().To<Presenter<TT>>()

            // Composition root
            .Root<IPresenter<TT>>("GetPresenter");

        var composition = new Composition();

        // The "model" argument is passed to the composition root
        // and then injected into the "Presenter" class
        var presenter = composition.GetPresenter<string>(model: "Hello World");

        presenter.Model.ShouldBe("Hello World");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IPresenter<out T>
{
    T? Model { get; }
}

class Presenter<T> : IPresenter<T>
{
    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public void Present(T model) =>
        Model = model;

    public T? Model { get; private set; }
}
// }