/*
$v=true
$p=17
$d=Bind generic contract attribute
$sa=Generic bind type attribute
$h=Shows how the built-in `BindAttribute` can declare a generic contract with the `TT` marker on a generic implementation. One attribute on `CardboardBox<TT>` then satisfies every closed instantiation the graph asks for.
$f=>[!NOTE]
$f=>`typeof(IBox<TT>)` is a marker-based generic contract. It is different from the open generic `typeof(IBox<>)`: Pure.DI uses `TT` to construct the matching implementation type, such as `CardboardBox<TT>`.
$f=
$f=Because the marker follows the requested type argument, `IBox<int>` and `IBox<string>` are both built from the same declaration, and the generated code is concrete for each of them. There is no open-generic registration to look up at run time.
$f=The contract has to be declared with the marker, not with `<>`, or the generator cannot match the implementation's own type parameters. Keep the number and order of markers aligned between contract and implementation.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Attributes.BindGenericContractAttributeScenario;

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
            .Bind<ICat>().To<ShroedingersCat>()

            // Composition root
            .Root<IBox<ICat>>("Box");

        var composition = new Composition();
        var box = composition.Box;

        box.ShouldBeOfType<CardboardBox<ICat>>();
        box.Content.ShouldBeOfType<ShroedingersCat>();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IBox<out T>
{
    T Content { get; }
}

interface ICat;

[Bind(typeof(IBox<TT>))]
record CardboardBox<T>(T Content) : IBox<T>;

class ShroedingersCat : ICat;
// }
