/*
$v=true
$p=15
$d=Bind type attributes
$sa=Bind type attribute
$h=Shows how several `Type` attributes can expose one implementation through several contracts.
$f=>[!NOTE]
$f=>Multiple type attributes in the same square-bracket binding group are merged into one binding with several contracts.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Attributes.MultipleBindTypeAttributesScenario;

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
            .Root<IMessageWriter>("Writer")
            .Root<IDiagnosticsSink>("Diagnostics");

        var composition = new Composition();

        composition.Writer.ShouldBeOfType<ConsoleChannel>();
        composition.Diagnostics.ShouldBeOfType<ConsoleChannel>();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IMessageWriter;

interface IDiagnosticsSink;

[Type(typeof(IMessageWriter))]
[Type(typeof(IDiagnosticsSink))]
class ConsoleChannel : IMessageWriter, IDiagnosticsSink;
// }
