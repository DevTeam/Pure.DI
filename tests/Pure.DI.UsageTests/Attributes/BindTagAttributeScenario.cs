/*
$v=true
$p=18
$d=Bind tag attribute
$sa=Bind attribute
$h=Shows how tags can be declared directly on implementation types, including several tags for one implementation. The tag becomes part of the binding, so a consumer can request the implementation through `[Tag(...)]` without the setup naming it.
$f=>[!NOTE]
$f=>A tag attribute on an implementation type becomes a binding tag. Several tag attributes in the same square-bracket binding group are merged into one binding, so the same implementation can be resolved by any of those tags.
$f=
$f=Several tags on one group are aliases for one instance: resolving by any of them returns the same binding and, with a singleton lifetime, the same object. That is useful when a legacy tag and a new tag must both keep working during a migration.
$f=Tags declared in separate groups create separate bindings rather than aliases, which changes collection injection. Keep aliases in one group and split only for genuinely different registrations.
$f=>[!TIP]
$f=>Prefer an enum or a [smart tag](smart-tags.md) over string literals once more than a couple of tags exist.
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
