/*
$v=true
$p=18
$d=Bind tag attribute
$sa=Bind attribute
$h=Shows how tags can be declared directly on implementation types, including several tags for one implementation.
$f=>[!NOTE]
$f=>A tag attribute on an implementation type becomes a binding tag. Several tag attributes in the same square-bracket binding group are merged into one binding, so the same implementation can be resolved by any of those tags.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Attributes.BindTagAttributeScenario;

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
            .Root<IMessageWriter>("FileWriter", "file");

        var composition = new Composition();

        composition.ConsoleWriter.ShouldBeOfType<ConsoleMessageWriter>();
        composition.DefaultWriter.ShouldBeOfType<ConsoleMessageWriter>();
        composition.FileWriter.ShouldBeOfType<FileMessageWriter>();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IMessageWriter;

[Tag("console")]
[Tag("default")]
class ConsoleMessageWriter : IMessageWriter;

[Tag("file")]
class FileMessageWriter : IMessageWriter;
// }
