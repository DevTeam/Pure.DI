/*
$v=true
$p=0
$d=Auto-bindings
$h=Injection of non-abstract types is possible without any additional effort. 
$f=:warning: But this approach cannot be recommended if you follow the dependency inversion principle and want your types to depend only on abstractions.
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
class Dependency;

class Service(Dependency dependency);
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {        
        // Specifies to create a partial class "Composition":
        DI.Setup("Composition")
            // Specifies to create a property "MyService":
            .Root<Service>("MyService");
        
        var composition = new Composition();

        // service = new Service(new Dependency());
        var service = composition.MyService;
// }
        composition.SaveClassDiagram();
    }
}