/*
$v=true
$p=1
$d=Simplified lifetime-specific bindings
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
        // This hint indicates to not generate methods such as Resolve
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