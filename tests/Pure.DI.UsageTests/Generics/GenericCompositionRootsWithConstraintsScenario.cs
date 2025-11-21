/*
$v=true
$p=4
$d=Generic composition roots with constraints
$h=> [!IMPORTANT]
$h=> `Resolve' methods cannot be used to resolve generic composition roots.
$f=> [!IMPORTANT]
$f=> The method `Inject()`cannot be used outside of the binding setup.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedTypeParameter

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Generics.GenericCompositionRootsWithConstraintsScenario;

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
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            .Bind().To<StreamSource<TTDisposable>>()
            .Bind().To<DataProcessor<TTDisposable, TTS>>()
            // Creates SpecializedDataProcessor manually,
            // just for the sake of example.
            // It treats 'bool' as the options type for specific boolean flags.
            .Bind("Specialized").To(ctx => {
                ctx.Inject(out IStreamSource<TTDisposable> source);
                return new SpecializedDataProcessor<TTDisposable>(source);
            })

            // Specifies to create a regular public method
            // to get a composition root of type DataProcessor<T, TOptions>
            // with the name "GetProcessor"
            .Root<IDataProcessor<TTDisposable, TTS>>("GetProcessor")

            // Specifies to create a regular public method
            // to get a composition root of type SpecializedDataProcessor<T>
            // with the name "GetSpecializedProcessor"
            // using the "Specialized" tag
            .Root<IDataProcessor<TTDisposable, bool>>("GetSpecializedProcessor", "Specialized");

        var composition = new Composition();

        // Creates a processor for a Stream with 'double' as options (e.g., threshold)
        // processor = new DataProcessor<Stream, double>(new StreamSource<Stream>());
        var processor = composition.GetProcessor<Stream, double>();

        // Creates a specialized processor for a BinaryReader
        // specializedProcessor = new SpecializedDataProcessor<BinaryReader>(new StreamSource<BinaryReader>());
        var specializedProcessor = composition.GetSpecializedProcessor<BinaryReader>();
        // }
        processor.ShouldBeOfType<DataProcessor<Stream, double>>();
        specializedProcessor.ShouldBeOfType<SpecializedDataProcessor<BinaryReader>>();
        composition.SaveClassDiagram();
    }
}

// {
interface IStreamSource<T>
    where T : IDisposable;

class StreamSource<T> : IStreamSource<T>
    where T : IDisposable;

interface IDataProcessor<T, TOptions>
    where T : IDisposable
    where TOptions : struct;

class DataProcessor<T, TOptions>(IStreamSource<T> source) : IDataProcessor<T, TOptions>
    where T : IDisposable
    where TOptions : struct;

class SpecializedDataProcessor<T>(IStreamSource<T> source) : IDataProcessor<T, bool>
    where T : IDisposable;
// }