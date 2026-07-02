/*
$v=true
$p=7
$d=Complex generic root arguments
$sa=Generic root arguments
$h=Root arguments can be generic too. `RootArg<SourceConfig<TT>>("config")` declares a root argument whose type follows the type parameter of the composition root, so the generated `GetSource<T>` method accepts a `SourceConfig<T>` at resolution time.
$h=This is useful when a generic service needs per-call configuration: here the config is delivered to `Source<T>` through the `Initialize` method marked with the `[Dependency]` attribute.
$f=>[!NOTE]
$f=>Complex generic scenarios require careful attention to type constraints and argument order for correct resolution.
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
// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.UsageTests.Generics.ComplexGenericRootArgScenario;

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
            // Defines a generic root argument 'config' of type SourceConfig<T>.
            // This allows passing specific configuration when resolving ISource<T>.
            .RootArg<SourceConfig<TT>>("config")
            .Bind<ISource<TT2>>().To<Source<TT2>>()

            // Composition root that creates a source for a specific type.
            // The 'GetSource' method will accept 'SourceConfig<T>' as an argument.
            .Root<ISource<TT3>>("GetSource");

        var composition = new Composition();

        // Resolve a source for 'int', passing specific configuration
        var source = composition.GetSource<int>(
            new SourceConfig<int>(33, "IntSource"));

        source.Value.ShouldBe(33);
        source.Name.ShouldBe("IntSource");
        // }
        composition.SaveClassDiagram();
    }
}

// {
// Represents configuration for a data source, including a default value
record SourceConfig<T>(T DefaultValue, string SourceName);

interface ISource<out T>
{
    T? Value { get; }
    string Name { get; }
}

class Source<T> : ISource<T>
{
    // The Dependency attribute specifies to perform an injection.
    // We use method injection to initialize the source with configuration
    // passed from the composition root.
    [Dependency]
    public void Initialize(SourceConfig<T> config)
    {
        Value = config.DefaultValue;
        Name = config.SourceName;
    }

    public T? Value { get; private set; }

    public string Name { get; private set; } = "";
}
// }