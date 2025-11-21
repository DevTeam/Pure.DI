/*
$v=true
$p=99
$d=Keyed service provider
$r=Shouldly;Microsoft.Extensions.DependencyInjection
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario;

using Microsoft.Extensions.DependencyInjection;
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
        var serviceProvider = new Composition();

        // Resolve the order service by key "Online".
        // This service expects a dependency with the key "PayPal".
        var orderService = serviceProvider.GetRequiredKeyedService<IOrderService>("Online");

        // Resolve the payment gateway by key "PayPal" to verify the correct injection
        var paymentGateway = serviceProvider.GetRequiredKeyedService<IPaymentGateway>("PayPal");

        // Check that the expected gateway instance was injected into the order service
        orderService.PaymentGateway.ShouldBe(paymentGateway);
// }
        serviceProvider.SaveClassDiagram();
    }
}

// {
// Payment gateway interface
interface IPaymentGateway;

// Payment gateway implementation (e.g., PayPal)
class PayPalGateway : IPaymentGateway;

// Order service interface
interface IOrderService
{
    IPaymentGateway PaymentGateway { get; }
}

// Implementation of the service for online orders.
// The [Tag("PayPal")] attribute indicates that an implementation 
// of IPaymentGateway registered with the key "PayPal" should be injected.
class OnlineOrderService([Tag("PayPal")] IPaymentGateway paymentGateway) : IOrderService
{
    public IPaymentGateway PaymentGateway { get; } = paymentGateway;
}

partial class Composition : IKeyedServiceProvider
{
    static void Setup() =>
        DI.Setup()
            // The following hint overrides the name of the
            // "object Resolve(Type type)" method in "GetService",
            // which implements the "IServiceProvider" interface
            .Hint(Hint.ObjectResolveMethodName, "GetService")
            // The following hint overrides the name of the
            // "object Resolve(Type type, object tag)" method in "GetRequiredKeyedService",
            // which implements the "IKeyedServiceProvider" interface
            .Hint(Hint.ObjectResolveByTagMethodName, "GetRequiredKeyedService")

            // Register PayPalGateway as a singleton with the key "PayPal"
            .Bind<IPaymentGateway>("PayPal").As(Lifetime.Singleton).To<PayPalGateway>()

            // Register OnlineOrderService with the key "Online"
            .Bind<IOrderService>("Online").To<OnlineOrderService>()

            // Composition roots available by keys
            .Root<IPaymentGateway>(tag: "PayPal")
            .Root<IOrderService>(tag: "Online");

    public object GetKeyedService(Type serviceType, object? serviceKey) =>
        GetRequiredKeyedService(serviceType, serviceKey);
}
// }