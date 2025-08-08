/*
$v=true
$p=0
$d=Auto-bindings
$h=Injection of non-abstract types is possible without any additional effort.
$f=> [!WARNING]
$f=> But this approach cannot be recommended if you follow the dependency inversion principle and want your types to depend only on abstractions. Or you want to precisely control the lifetime of a dependency.
$f=
$f=It is better to inject abstract dependencies, for example, in the form of interfaces. Use bindings to map abstract types to their implementations as in almost all [other examples](injections-of-abstractions.md).
*/

// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.AutoBindingsScenario;

using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {        
        // Specifies to create a partial class with name "Composition"
        DI.Setup("Composition")
            // with the root "MyService"
            .Root<Service>("MyService");

        var composition = new Composition();

        // service = new Service(new Dependency())
        var service = composition.MyService;
// }
        composition.SaveClassDiagram();
    }
}

// {
class Dependency;

class Service(Dependency dependency);
// }