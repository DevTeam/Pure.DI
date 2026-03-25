/*
$v=true
$p=0
$d=Auto-bindings
$h=Pure.DI can create non-abstract types without explicit bindings, which makes quick prototypes and small demos concise.
$h=The generator still validates the graph at compile time and produces regular C# object creation code.
$f=Auto-bindings are convenient for utilities and sample code where dependency choices are obvious.
$f=In larger applications they can hide architectural intent, because consumers start depending on concrete classes.
$f=If you need interchangeable implementations and explicit lifetime control, prefer bindings of abstractions to implementations.
$f=>[!WARNING]
$f=>This approach is not recommended if you follow the dependency inversion principle or need precise lifetime control.
$f=
$f=Prefer injecting abstractions (for example, interfaces) and map them to implementations as in most [other examples](injections-of-abstractions.md).
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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        // Specifies to create a partial class named "Composition"
        DI.Setup("Composition")
            // with the root "Orders"
            .Root<OrderService>("Orders");

        var composition = new Composition();

        // service = new OrderService(new Database())
        var orders = composition.Orders;
// }
        composition.SaveClassDiagram();
    }
}

// {
class Database;

class OrderService(Database database);
// }
