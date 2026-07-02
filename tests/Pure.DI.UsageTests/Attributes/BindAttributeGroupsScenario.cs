/*
$v=true
$p=21
$d=Bind attribute groups
$sa=Bind metadata merge
$h=Shows how separate `BindAttribute` groups on one implementation type create separate bindings.
$f=>[!NOTE]
$f=>Attributes inside one square-bracket group are merged into one binding. To create several bindings for the same implementation type, place `Bind` attributes in separate square-bracket groups.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Attributes.BindAttributeGroupsScenario;

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
            .Root<IMessageWriter>("AuditWriter", "audit");

        var composition = new Composition();
        var consoleWriter = composition.ConsoleWriter;
        var auditWriter = composition.AuditWriter;

        consoleWriter.ShouldBeOfType<MessageWriter>();
        auditWriter.ShouldBeOfType<MessageWriter>();
        auditWriter.ShouldNotBeSameAs(consoleWriter);
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IMessageWriter
{
    void Write(string message);
}

[Bind(typeof(IMessageWriter), Lifetime.Singleton, "console")]
[Bind(typeof(IMessageWriter), Lifetime.Transient, "audit")]
class MessageWriter : IMessageWriter
{
    public void Write(string message) => Console.WriteLine(message);
}
// }
