/*
$v=true
$t=QuickStart
$t=Basics
$p=0
$p=QuickStart:0
$d=Auto-bindings
$sa=Injections of abstractions
$sa=Simplified binding
$h=Pure.DI can create non-abstract types without explicit bindings, which makes quick prototypes and small demos concise.
$h=![](auto-binding.gif)
$h=The generator still validates the graph at compile time and produces regular C# object creation code.
$f=Auto-bindings fit tools, samples and small internal utilities where the concrete class is the design choice.
$f=The catch is architectural: a consumer that asks for a class cannot be swapped for another implementation without editing that consumer, and its lifetime becomes transient unless you bind it explicitly.
$f=>[!WARNING]
$f=>This approach is not recommended if you follow the dependency inversion principle or need precise lifetime control.
$f=
$f=Prefer injecting abstractions (for example, interfaces) and map them to implementations as in most [other examples](injections-of-abstractions.md).
$f=Limitations: auto-bindings scale poorly when several implementations, decorators, or strict lifetime rules are required; `Hint(Hint.DisableAutoBinding, "On")` makes the omission an error instead of a silent default.
$f=See also: [Injections of abstractions](injections-of-abstractions.md), [Simplified binding](simplified-binding.md).
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
