/*
$v=true
$p=14
$d=Bind type attribute
$sa=Bind attribute
$sa=Bind type attributes
$h=Shows how the `Type` attribute can declare a contract directly on an implementation type. It is the attribute counterpart of `.Bind<IContract>().To<T>()`, and the implementation names the abstraction it serves.
$f=>[!NOTE]
$f=>When a registered type attribute is applied to a class or struct, Pure.DI treats it as binding metadata for that implementation.
$f=
$f=`Type` without a lifetime leaves the binding transient, which is the right default for a stateless writer that is created per root. Add `Lifetime(...)` or `Bind(...)` in the same group when the implementation must be shared.
$f=Only the declared contract is registered: the graph can inject `IMessageWriter` but not the concrete `ConsoleMessageWriter` unless another binding exposes it. That keeps the graph surface equal to the abstractions the application really consumes.
$f=>[!TIP]
$f=>Reach for `Type` when an implementation assembly should own its contract, and keep the setup for choices that one application makes differently.
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
