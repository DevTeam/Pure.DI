/*
$v=true
$p=14
$d=Bind type attribute
$sa=Bind attribute
$sa=Bind type attributes
$h=Shows how the `Type` attribute can declare a contract directly on an implementation type.
$f=>[!NOTE]
$f=>When a registered type attribute is applied to a class or struct, Pure.DI treats it as binding metadata for that implementation.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Attributes.BindTypeAttributeScenario;

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

            // Composition root
            .Root<IMessageWriter>("Writer");

        var composition = new Composition();
        var writer = composition.Writer;

        writer.ShouldBeOfType<ConsoleMessageWriter>();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IMessageWriter;

[Type(typeof(IMessageWriter))]
class ConsoleMessageWriter : IMessageWriter;
// }
