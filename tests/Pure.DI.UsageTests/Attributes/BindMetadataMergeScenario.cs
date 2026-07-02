/*
$v=true
$p=20
$d=Bind metadata merge
$sa=Bind attribute groups
$h=Shows how binding metadata attributes on one implementation type are combined into one binding.
$f=>[!NOTE]
$f=>Attributes inside the same square-bracket group form one binding. Contracts and tags are merged, but lifetime must be specified at most once in the group. Separate `Bind` attribute groups create separate bindings.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Attributes.BindMetadataMergeScenario;

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

            // Composition roots
            .Root<IMessageWriter>("ConsoleWriter", "console")
            .Root<IMessageWriter>("DefaultWriter", "default")
            .Root<IDiagnosticsSink>("ConsoleDiagnostics", "console");

        var composition = new Composition();

        composition.ConsoleWriter.ShouldBeOfType<ConsoleChannel>();
        composition.DefaultWriter.ShouldBeSameAs(composition.ConsoleWriter);
        composition.ConsoleDiagnostics.ShouldBeSameAs(composition.ConsoleWriter);
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IMessageWriter;

interface IDiagnosticsSink;

[Bind(typeof(IMessageWriter)), Bind(typeof(IDiagnosticsSink)), Tag("console"), Tag("default"), Lifetime(Lifetime.Singleton)]
class ConsoleChannel : IMessageWriter, IDiagnosticsSink;
// }
