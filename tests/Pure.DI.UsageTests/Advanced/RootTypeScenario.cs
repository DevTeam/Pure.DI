/*
$v=true
$p=11
$d=Root Type
$h=`RootType` provides the type of the composition root being resolved. This property is useful for implementing root-specific behavior like different caching strategies per root type.
$h=Use this when infrastructure dependencies must vary by root contract type.
$f=Limitations: root-type-specific rules can become hidden policy; keep this logic centralized and observable.
$f=See also: [Composition roots](composition-roots.md), [Root Name](root-name.md).
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable NotAccessedPositionalProperty.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.RootTypeScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        var composition = new Composition();
        var orderService = composition.OrderService;
        orderService.Cache.Set("order_123", "Order Data");
        orderService.Cache.Get("order_123").ShouldBe("Order Data");
        orderService.Cache.KeyPrefix.ShouldBe("IOrderService");

        var inventoryService = composition.InventoryService;
        inventoryService.Cache.Set("item_456", "Item Data");
        inventoryService.Cache.Get("item_456").ShouldBe("Item Data");
        inventoryService.Cache.KeyPrefix.ShouldBe("IInventoryService");
// }

        composition.SaveClassDiagram();
    }
}

// {
interface ICache
{
    string KeyPrefix { get; }

    void Set(string key, string value);

    string Get(string key);
}

interface IOrderService
{
    ICache Cache { get; }
}

interface IInventoryService
{
    ICache Cache { get; }
}

class Cache(Type rootType) : ICache
{
    private readonly Dictionary<string, string> _data = new();

    public string KeyPrefix => rootType.Name;

    public void Set(string key, string value) => _data[key] = value;

    public string Get(string key) => _data.TryGetValue(key, out var value) ? value : string.Empty;
}

class OrderService(ICache cache) : IOrderService
{
    public ICache Cache => cache;
}

class InventoryService(ICache cache) : IInventoryService
{
    public ICache Cache => cache;
}

partial class Composition
{
    private void Setup() =>
// }
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind().To(ctx => new Cache(ctx.RootType))
            .Bind().To<OrderService>()
            .Root<IOrderService>("OrderService")
            .Bind().To<InventoryService>()
            .Root<IInventoryService>("InventoryService");
}
// }
