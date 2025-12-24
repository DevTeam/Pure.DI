/*
$v=true
$p=10
$d=Partial class
$h=A partial class can contain setup code.
$f=The partial class is also useful for specifying access modifiers to the generated class.
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.PartialClassScenario;

using System.Diagnostics;
using Shouldly;
using Xunit;
using static RootKinds;

// {
//# using Pure.DI;
//# using static Pure.DI.RootKinds;
//# using System.Diagnostics;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        var composition = new Composition("NorthStore");
        var orderService = composition.OrderService;

        // Checks that the dependencies were created correctly
        orderService.Order1.Id.ShouldBe(1);
        orderService.Order2.Id.ShouldBe(2);
        // Checks that the injected string contains the store name and the generated ID
        orderService.OrderDetails.ShouldBe("NorthStore_3");
// }
        orderService.ShouldBeOfType<OrderService>();
        composition.SaveClassDiagram();
    }
}

// {
interface IOrder
{
    long Id { get; }
}

class Order(long id) : IOrder
{
    public long Id { get; } = id;
}

class OrderService(
    [Tag("Order details")] string details,
    IOrder order1,
    IOrder order2)
{
    public string OrderDetails { get; } = details;

    public IOrder Order1 { get; } = order1;

    public IOrder Order2 { get; } = order2;
}

// The partial class is also useful for specifying access modifiers to the generated class
public partial class Composition(string storeName)
{
    private long _id;

    private long GenerateId() => Interlocked.Increment(ref _id);

    // In fact, this method will not be called at runtime
    [Conditional("DI")]
    void Setup() =>
// }
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup()
            .Bind<IOrder>().To<Order>()
            .Bind<long>().To(_ => GenerateId())
            // Binds the string with the tag "Order details"
            .Bind<string>("Order details").To(_ => $"{storeName}_{GenerateId()}")
            .Root<OrderService>("OrderService", kind: Internal);
}
// }