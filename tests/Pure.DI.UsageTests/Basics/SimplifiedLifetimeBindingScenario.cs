/*
$v=true
$p=8
$d=Simplified lifetime-specific bindings
$h=You can use the `Transient<>()`, `Singleton<>()`, `PerResolve<>()`, etc. methods. In this case binding will be performed for the implementation type itself, and if the implementation is not an abstract type or structure, for all abstract but NOT special types that are directly implemented.
$h=This keeps lifetime configuration concise while preserving explicit lifetime semantics.
$f=These methods perform the binding with appropriate lifetime:
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
$f=If you want to add your own special type, use the `SpecialType<T>()` call.
$f=
$f=For class `OrderManager`, the `PerBlock<OrderManager>()` binding will be equivalent to the `Bind<IOrderRepository, IOrderNotification, OrderManager>().As(Lifetime.PerBlock).To<OrderManager>()` binding. The types `IDisposable`, `IEnumerable<string>` did not get into the binding because they are special from the list above. `ManagerBase` did not get into the binding because it is not abstract. `IManager` is not included because it is not implemented directly by class `OrderManager`.
$f=
$f=|    |                       |                                                   |
$f=|----|-----------------------|---------------------------------------------------|
$f=| yes | `OrderManager`        | implementation type itself                        |
$f=| yes | `IOrderRepository`    | directly implements                               |
$f=| yes | `IOrderNotification`  | directly implements                               |
$f=| no  | `IDisposable`         | special type                                      |
$f=| no  | `IEnumerable<string>` | special type                                      |
$f=| no  | `ManagerBase`         | non-abstract                                      |
$f=| no  | `IManager`            | is not directly implemented by class OrderManager |
$f=Limitations: lifetime-specific shortcuts still rely on inferred contracts, so review inferred bindings carefully.
$f=Common pitfalls:
$f=- Applying singleton shortcuts to stateful services without thread-safety guarantees.
$f=- Assuming shortcut APIs bypass special-type exclusion rules.
$f=See also: [Transient](transient.md), [Simplified binding](simplified-binding.md).
*/

// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.SimplifiedLifetimeBindingScenario;

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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        // Specifies to create a partial class "Composition"
        DI.Setup(nameof(Composition))
            // The equivalent of the following:
            // .Bind<IOrderRepository, IOrderNotification, OrderManager>()
            //   .As(Lifetime.PerBlock)
            //   .To<OrderManager>()
            .PerBlock<OrderManager>()
            // The equivalent of the following:
            // .Bind<IShop, Shop>()
            //   .As(Lifetime.Transient)
            //   .To<Shop>()
            // .Bind<IOrderNameFormatter, OrderNameFormatter>()
            //   .As(Lifetime.Transient)
            //   .To<OrderNameFormatter>()
            .Transient<Shop, OrderNameFormatter>()

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

class OrderManager(IOrderNameFormatter orderNameFormatter) :
    ManagerBase,
    IOrderRepository,
    IOrderNotification,
    IDisposable,
    IEnumerable<string>
{
    public void Dispose() {}

    public IEnumerator<string> GetEnumerator() =>
        new List<string>
        {
            orderNameFormatter.Format(1),
            orderNameFormatter.Format(2)
        }.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

interface IOrderNameFormatter
{
    string Format(int orderId);
}

class OrderNameFormatter : IOrderNameFormatter
{
    public string Format(int orderId) => $"Order #{orderId}";
}

interface IShop;

class Shop(
    OrderManager manager,
    IOrderRepository repository,
    IOrderNotification notification)
    : IShop;
// }
