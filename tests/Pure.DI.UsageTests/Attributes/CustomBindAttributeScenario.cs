/*
$v=true
$p=13
$d=Custom bind attribute
$sa=Bind attribute
$h=Shows how to declare a binding directly on an implementation type with a custom attribute. A custom attribute can combine contract type, lifetime, and tag metadata by registering argument positions in the composition setup.
$f=>[!NOTE]
$f=>Implementation-level binding attributes are useful when the implementation assembly should describe its DI role while keeping the composition concise. Custom attributes participate in the same merge rules as the built-in `Bind`, `Type`, `Tag`, and `Lifetime` attributes: attributes in one square-bracket group form one binding, while separate `Bind` groups create separate bindings.
$f=
$f=Because the attribute arguments are mapped by position in the setup, one custom attribute can carry contract, lifetime and tag together, and the class declaration stays readable. The registration tells the generator which argument holds what.
$f=Positional mapping is brittle when the attribute constructor changes: reordering parameters changes what every existing use means. Keep the constructor order stable or register explicit indices.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedTypeParameter
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Attributes.CustomBindAttributeScenario;

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
            .TypeAttribute<ServiceAttribute<TT>>()
            .LifetimeAttribute<ServiceAttribute<TT>>()
            .TagAttribute<ServiceAttribute<TT>>(1)

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
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
class ServiceAttribute<T> : Attribute
{
    public ServiceAttribute(
        Lifetime lifetime = Lifetime.Transient,
        object? tag = null)
    {
    }
}

interface IMessageWriter
{
    void Write(string message);
}

[Service<IMessageWriter>(Lifetime.Singleton, "console")]
class ConsoleMessageWriter : IMessageWriter
{
    public void Write(string message) => Console.WriteLine(message);
}
// }
