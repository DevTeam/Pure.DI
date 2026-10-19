/*
$v=true
$p=12
$d=Bind attribute
$sa=Bind type attribute
$sa=Bind lifetime attribute
$sa=Bind tag attribute
$h=Shows how to declare a binding directly on an implementation type with the built-in `BindAttribute`. The implementation carries its contract, lifetime and tag, and the setup only lists composition roots, so adding a service does not mean editing the composition.
$f=>[!NOTE]
$f=>`BindAttribute` is registered by default and can provide the contract type, lifetime, and tag. Attributes inside the same square-bracket group form one binding; separate `Bind` groups create separate bindings.
$f=
$f=The lifetime in the attribute is the same metadata as `.As(Singleton)`, so a reader of `ConsoleMessageWriter` sees that the whole composition shares one instance, and the root assertion in this scenario confirms it. Attribute metadata is read by the generator at compile time and costs nothing at run time.
$f=Because the contract is given as `typeof(IMessageWriter)`, a mistyped contract is a compilation error, and a contract that no binding satisfies is reported while the code is compiled rather than when it runs.
$f=>[!TIP]
$f=>Prefer the setup API for bindings that only one application needs, and attribute bindings for implementations whose DI role is stable across every consumer.
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
