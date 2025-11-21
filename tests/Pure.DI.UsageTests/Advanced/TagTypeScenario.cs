/*
$v=true
$p=3
$d=Tag Type
$h=`Tag.Type` in bindings replaces the expression `typeof(T)`, where `T` is the type of the implementation in a binding.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable PreferConcreteValueOverDefault
namespace Pure.DI.UsageTests.Advanced.TagTypeScenario;

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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
        // {
        DI.Setup(nameof(Composition))
            // Tag.Type here is the same as typeof(CreditCardGateway)
            // The `default` tag is used to resolve dependencies
            // when the tag was not specified by the consumer
            .Bind<IPaymentGateway>(Tag.Type, default).To<CreditCardGateway>()
            // Tag.Type here is the same as typeof(PayPalGateway)
            .Bind<IPaymentGateway>(Tag.Type).As(Lifetime.Singleton).To<PayPalGateway>()
            .Bind<IPaymentProcessor>().To<PaymentProcessor>()

            // Composition root
            .Root<IPaymentProcessor>("PaymentSystem")

            // "PayPalRoot" is root name, typeof(PayPalGateway) is tag
            .Root<IPaymentGateway>("PayPalRoot", typeof(PayPalGateway));

        var composition = new Composition();
        var service = composition.PaymentSystem;
        service.PrimaryGateway.ShouldBeOfType<CreditCardGateway>();
        service.AlternativeGateway.ShouldBeOfType<PayPalGateway>();
        service.AlternativeGateway.ShouldBe(composition.PayPalRoot);
        service.DefaultGateway.ShouldBeOfType<CreditCardGateway>();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IPaymentGateway;

class CreditCardGateway : IPaymentGateway;

class PayPalGateway : IPaymentGateway;

interface IPaymentProcessor
{
    IPaymentGateway PrimaryGateway { get; }

    IPaymentGateway AlternativeGateway { get; }

    IPaymentGateway DefaultGateway { get; }
}

class PaymentProcessor(
    [Tag(typeof(CreditCardGateway))] IPaymentGateway primaryGateway,
    [Tag(typeof(PayPalGateway))] IPaymentGateway alternativeGateway,
    IPaymentGateway defaultGateway)
    : IPaymentProcessor
{
    public IPaymentGateway PrimaryGateway { get; } = primaryGateway;

    public IPaymentGateway AlternativeGateway { get; } = alternativeGateway;

    public IPaymentGateway DefaultGateway { get; } = defaultGateway;
}
// }