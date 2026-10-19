/*
$v=true
$p=5
$d=Custom generic argument
$sa=Custom generic argument attribute
$sa=Generics
$h=Besides the built-in marker types like `TT`, `TT1`, `TTS`, you can declare your own. Registering a type with `GenericTypeArgument<MyTT>()` turns it into a marker usable in generic bindings, such as `Bind<ISequence<MyTT>>().To<Sequence<MyTT>>()`.
$h=Reach for this when the predefined markers are not enough — for example, to give markers meaningful names or specific type constraints.
$f=>[!NOTE]
$f=>Custom generic arguments provide flexibility for complex generic scenarios beyond standard marker types.
$f=
$f=A project with several generic subsystems benefits from markers that say what they mean: a repository marker, a key marker, an id marker. It also lets a marker satisfy a constraint the built-in ones cannot, such as `struct` or a specific interface.
$f=The registration is per composition: `.GenericTypeArgument<MyTT>()` must appear in every setup that uses `MyTT` in a binding, otherwise the type is treated as a regular type argument and resolution fails.
$f=>[!NOTE]
$f=>The marker itself is never instantiated. Declare it abstract or as an interface so nothing can accidentally depend on a real implementation of it.
$f=
$f=Prefer the built-in `TT`, `TT1` and `TTS` for one-off bindings, and a named marker when the same type parameter is used in several bindings or must carry a constraint. See [Custom generic argument attribute](custom-generic-argument-attribute.md) to declare the marker without registering it in the setup.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedTypeParameter
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable InconsistentNaming

namespace Pure.DI.UsageTests.Generics.CustomGenericArgumentScenario;

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
            // Registers the "MyTT" interface as a custom generic type argument
            // to be used as a marker for generic bindings
            .GenericTypeArgument<MyTT>()
            .Bind<ISequence<MyTT>>().To<Sequence<MyTT>>()
            .Bind<IProgram>().To<MyApp>()

            // Composition root
            .Root<IProgram>("Root");

        var composition = new Composition();
        var program = composition.Root;
        program.IntSequence.ShouldBeOfType<Sequence<int>>();
        program.StringSequence.ShouldBeOfType<Sequence<string>>();
// }
        composition.SaveClassDiagram();
    }
}

// {
// Defines a custom generic type argument marker
interface MyTT;

interface ISequence<T>;

class Sequence<T> : ISequence<T>;

interface IProgram
{
    ISequence<int> IntSequence { get; }

    ISequence<string> StringSequence { get; }
}

class MyApp(
    ISequence<int> intSequence,
    ISequence<string> stringSequence)
    : IProgram
{
    public ISequence<int> IntSequence { get; } = intSequence;

    public ISequence<string> StringSequence { get; } = stringSequence;
}
// }