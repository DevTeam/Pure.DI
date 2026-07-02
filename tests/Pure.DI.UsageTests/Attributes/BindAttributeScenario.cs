/*
$v=true
$p=12
$d=Bind attribute
$sa=Bind type attribute
$sa=Bind lifetime attribute
$sa=Bind tag attribute
$h=Shows how to declare a binding directly on an implementation type with the built-in `BindAttribute`.
$f=>[!NOTE]
$f=>`BindAttribute` is registered by default and can provide the contract type, lifetime, and tag. Attributes inside the same square-bracket group form one binding; separate `Bind` groups create separate bindings.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Attributes.BindAttributeScenario;

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
            .Root<IMessageWriter>("Writer", "console");

        var composition = new Composition();
        var writer = composition.Writer;
        writer.Write("Pure.DI");

        writer.ShouldBeOfType<ConsoleMessageWriter>();
        writer.ShouldBeSameAs(composition.Writer);
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
class ConsoleMessageWriter : IMessageWriter
{
    public void Write(string message) => Console.WriteLine(message);
}
// }
