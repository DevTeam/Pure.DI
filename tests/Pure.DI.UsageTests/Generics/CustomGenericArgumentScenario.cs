/*
$v=true
$p=6
$d=Custom generic argument
$h=Besides the built-in marker types like `TT`, `TT1`, `TTS`, you can declare your own. Registering a type with `GenericTypeArgument<MyTT>()` turns it into a marker usable in generic bindings, such as `Bind<ISequence<MyTT>>().To<Sequence<MyTT>>()`.
$h=Reach for this when the predefined markers are not enough — for example, to give markers meaningful names or specific type constraints.
$f=>[!NOTE]
$f=>Custom generic arguments provide flexibility for complex generic scenarios beyond standard marker types.
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