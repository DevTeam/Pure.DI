/*
$v=true
$p=16
$d=Generic bind type attribute
$sa=Bind generic contract attribute
$h=Shows how a custom generic attribute can declare a contract type on an implementation.
$f=>[!NOTE]
$f=>Registering the custom generic attribute with `TypeAttribute<T>()` lets Pure.DI read the contract from the attribute type argument.
$f=
$f=A generic attribute is useful when the contract itself varies per member and you do not want to repeat `typeof(...)` in every declaration. The attribute type argument is the contract, so `[MyGenericType<IMessageWriter>]` and `[MyGenericType<IDiagnosticsSink>]` can sit on two members of the same type.
$f=>[!WARNING]
$f=>The attribute has to be registered with the same generic shape it is declared with, otherwise the generator does not see it and the member is silently not injected.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedTypeParameter
namespace Pure.DI.UsageTests.Attributes.GenericBindTypeAttributeScenario;

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
            .TypeAttribute<BindingAttribute<TT>>()

            // Composition root
            .Root<IMessageWriter>("Writer");

        var composition = new Composition();
        var writer = composition.Writer;

        writer.ShouldBeOfType<FileMessageWriter>();
        // }
        composition.SaveClassDiagram();
    }
}

// {
[AttributeUsage(AttributeTargets.Class)]
class BindingAttribute<T> : Attribute;

interface IMessageWriter;

[Binding<IMessageWriter>]
class FileMessageWriter : IMessageWriter;
// }
