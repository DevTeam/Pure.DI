/*
$v=true
$p=19
$d=Bind lifetime attribute
$sa=Bind attribute
$h=Shows how the `Lifetime` attribute can declare the lifetime of an implementation binding. The lifetime travels with the implementation, next to the contract it was bound to.
$f=>[!NOTE]
$f=>A lifetime attribute on an implementation type is equivalent to applying `.As(...)` to the generated binding. Lifetime metadata can be specified only once inside one square-bracket binding group; repeated lifetime metadata in the same group is a compilation error.
$f=
$f=This is the attribute form of `.As(Singleton)`, so it decides how long the instance lives for the whole composition, not per injection site. Readers of the implementation see the lifetime without opening the setup.
$f=A lifetime on the class applies to every binding declared in that group. When one implementation needs two lifecycles, split the registrations into separate groups instead of trying to override the attribute from the setup.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Attributes.BindLifetimeAttributeScenario;

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
            .Root<IClock>("Clock");

        var composition = new Composition();

        composition.Clock.ShouldBeSameAs(composition.Clock);
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IClock;

[Type(typeof(IClock))]
[Lifetime(Lifetime.Singleton)]
class SystemClock : IClock;
// }
