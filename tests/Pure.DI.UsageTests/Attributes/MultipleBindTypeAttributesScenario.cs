/*
$v=true
$p=15
$d=Bind type attributes
$sa=Bind type attribute
$h=Shows how several `Type` attributes can expose one implementation through several contracts. The implementation declares every abstraction it implements once, and each abstraction can be injected on its own.
$f=>[!NOTE]
$f=>Multiple type attributes in the same square-bracket binding group are merged into one binding with several contracts.
$f=
$f=This is the attribute spelling of `.Bind<IOne>().Bind<ITwo>().To<T>()`, and it removes repeated chains from the setup. The composition only keeps roots for the contracts that outside code consumes.
$f=Because the contracts share one group, adding a contract changes the same binding and the same instance is reused across the abstractions. A separate group would create a second binding of the same type, so a collection would receive two entries where one was intended.
$f=>[!TIP]
$f=>Prefer this when the implementation assembly owns its contracts, and the setup API when the choice belongs to one application.
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
