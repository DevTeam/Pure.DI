/*
$v=true
$p=20
$d=Bind lifetime attribute
$h=Shows how the `Lifetime` attribute can declare the lifetime of an implementation binding.
$f=>[!NOTE]
$f=>A lifetime attribute on an implementation type is equivalent to applying `.As(...)` to the generated binding. Lifetime metadata can be specified only once inside one square-bracket binding group; repeated lifetime metadata in the same group is a compilation error.
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
