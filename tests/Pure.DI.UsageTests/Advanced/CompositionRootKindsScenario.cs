/*
$v=true
$p=0
$d=Composition root kinds
$sa=Composition roots
$sa=Static root
$h=By default, a composition root is a public instance property, but the `kind` argument of `Root<T>(...)` lets you change that. Combine `RootKinds` flags to generate the root as a method instead of a property, adjust its visibility (`Public`, `Internal`, `Private`), or make it `Static` or `Partial`.
$h=A private partial root is useful when you want to wrap the generated code in your own hand-written member, as the `PaymentService` property does here.
$f=>[!NOTE]
$f=>Composition roots can be customized with different kinds to control accessibility and lifetime, enabling flexible API design patterns.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario;

using Shouldly;
using Xunit;
using static RootKinds;

// {
//# using Pure.DI;
//# using static Pure.DI.RootKinds;
// }
public class Scenario
{
    [Fact]
    public void Run()
    {
        // {
        var composition = new Composition();
        var paymentService = composition.PaymentService;
        var cashPaymentService = composition.GetCashPaymentService();
        var validator = Composition.Validator;
        // }
        paymentService.ShouldBeOfType<CardPaymentService>();
        cashPaymentService.ShouldBeOfType<CashPaymentService>();
        validator.ShouldBeOfType<LuhnValidator>();
        composition.SaveClassDiagram();
    }
}

// {
interface ICreditCardValidator;

class LuhnValidator : ICreditCardValidator;

interface IPaymentService;

class CardPaymentService : IPaymentService
{
    public CardPaymentService(ICreditCardValidator validator)
    {
    }
}

class CashPaymentService : IPaymentService;

partial class Composition
{
    void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IPaymentService>().To<CardPaymentService>()
            .Bind<IPaymentService>("Cash").To<CashPaymentService>()
            .Bind<ICreditCardValidator>().To<LuhnValidator>()

            // Creates a public root method named "GetCashPaymentService"
            .Root<IPaymentService>("GetCashPaymentService", "Cash", Public | Method)

            // Creates a private partial root method named "GetCardPaymentService"
            .Root<IPaymentService>("GetCardPaymentService", kind: Private | Partial | Method)

            // Creates an internal static root named "Validator"
            .Root<ICreditCardValidator>("Validator", kind: Internal | Static);

    private partial IPaymentService GetCardPaymentService();

    public IPaymentService PaymentService => GetCardPaymentService();
}
// }