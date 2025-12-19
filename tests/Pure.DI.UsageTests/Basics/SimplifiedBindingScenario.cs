/*
$v=true
$p=1
$d=Simplified binding
$h=You can use the `Bind(...)` method without type parameters. In this case binding will be performed for the implementation type itself, and if the implementation is not an abstract type or structure, for all abstract but NOT special types that are directly implemented.
$f=As practice has shown, in most cases it is possible to define abstraction types in bindings automatically. That's why we added API `Bind()` method without type parameters to define abstractions in bindings. It is the `Bind()` method that performs the binding:
$f=
$f=- with the implementation type itself
$f=- and if it is NOT an abstract type or structure
$f=  - with all abstract types that it directly implements
$f=  - exceptions are special types
$f=
$f=Special types will not be added to bindings:
$f=
$f=- `System.Object`
$f=- `System.Enum`
$f=- `System.MulticastDelegate`
$f=- `System.Delegate`
$f=- `System.Collections.IEnumerable`
$f=- `System.Collections.Generic.IEnumerable<T>`
$f=- `System.Collections.Generic.IList<T>`
$f=- `System.Collections.Generic.ICollection<T>`
$f=- `System.Collections.IEnumerator`
$f=- `System.Collections.Generic.IEnumerator<T>`
$f=- `System.Collections.Generic.IReadOnlyList<T>`
$f=- `System.Collections.Generic.IReadOnlyCollection<T>`
$f=- `System.IDisposable`
$f=- `System.IAsyncResult`
$f=- `System.AsyncCallback`
$f=
$f=For class `OrderManager`, the `Bind().To<OrderManager>()` binding will be equivalent to the `Bind<IOrderRepository, IOrderNotification, OrderManager>().To<OrderManager>()` binding. The types `IDisposable`, `IEnumerable<string>` did not get into the binding because they are special from the list above. `ManagerBase` did not get into the binding because it is not abstract. `IManager` is not included because it is not implemented directly by class `OrderManager`.
$f=
$f=|    |                       |                                                   |
$f=|----|-----------------------|---------------------------------------------------|
$f=| ✅ | `OrderManager`        | implementation type itself                        |
$f=| ✅ | `IOrderRepository`    | directly implements                               |
$f=| ✅ | `IOrderNotification`  | directly implements                               |
$f=| ❌ | `IDisposable`         | special type                                      |
$f=| ❌ | `IEnumerable<string>` | special type                                      |
$f=| ❌ | `ManagerBase`         | non-abstract                                      |
$f=| ❌ | `IManager`            | is not directly implemented by class OrderManager |
*/

// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.SimplifiedBindingScenario;

using System.Collections;
using Xunit;

// {
//# using System.Collections;
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
        // Specifies to create a partial class "Composition"
        DI.Setup(nameof(Composition))
            // Begins the binding definition for the implementation type itself,
            // and if the implementation is not an abstract class or structure,
            // for all abstract but NOT special types that are directly implemented.
            // So that's the equivalent of the following:
            // .Bind<IOrderRepository, IOrderNotification, OrderManager>()
            //   .As(Lifetime.PerBlock)
            //   .To<OrderManager>()
            .Bind().As(Lifetime.PerBlock).To<OrderManager>()
            .Bind().To<Shop>()

            // Specifies to create a property "MyShop"
            .Root<IShop>("MyShop");

        var composition = new Composition();
        var shop = composition.MyShop;
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IManager;

class ManagerBase : IManager;

interface IOrderRepository;

interface IOrderNotification;

class OrderManager :
    ManagerBase,
    IOrderRepository,
    IOrderNotification,
    IDisposable,
    IEnumerable<string>
{
    public void Dispose() {}

    public IEnumerator<string> GetEnumerator() =>
        new List<string> { "Order #1", "Order #2" }.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

interface IShop;

class Shop(
    OrderManager manager,
    IOrderRepository repository,
    IOrderNotification notification)
    : IShop;
// }