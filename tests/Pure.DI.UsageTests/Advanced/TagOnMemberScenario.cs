/*
$v=true
$p=6
$d=Tag on a member
$h=The wildcards ‘*’ and ‘?’ are supported.
$f=> [!WARNING]
$f=> Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedTypeParameter

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.TagOnMemberScenario;

using System.Diagnostics.CodeAnalysis;
using Pure.DI;
using UsageTests;
using Xunit;

// {
//# using Pure.DI;
// }

[SuppressMessage("WRN", "DIW001:WRN")]
public class Scenario
{
    [Fact]
    public void Run()
    {
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind().To<PayPalGateway>()
            // Binds StripeGateway to the "Gateway" property of the "CheckoutService" class.
            // This allows you to override the injected dependency for a specific member
            // without changing the class definition.
            .Bind(Tag.OnMember<CheckoutService>(nameof(CheckoutService.Gateway)))
            .To<StripeGateway>()
            .Bind<ICheckoutService>().To<CheckoutService>()

            // Specifies to create the composition root named "Root"
            .Root<ICheckoutService>("CheckoutService");

        var composition = new Composition();
        var checkoutService = composition.CheckoutService;

        // Checks that the property was injected with the specific implementation
        checkoutService.Gateway.ShouldBeOfType<StripeGateway>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IPaymentGateway;

class PayPalGateway : IPaymentGateway;

class StripeGateway : IPaymentGateway;

interface ICheckoutService
{
    IPaymentGateway Gateway { get; }
}

class CheckoutService : ICheckoutService
{
    public required IPaymentGateway Gateway { init; get; }
}
// }