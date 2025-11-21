/*
$v=true
$p=6
$d=Custom generic argument
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
        // This hint indicates to not generate methods such as Resolve
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