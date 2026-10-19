/*
$v=true
$p=0
$d=Composition root kinds
$sa=Composition roots
$sa=Static root
$h=By default, a composition root is a public instance property, but the `kind` argument of `Root<T>(...)` lets you change that. Combine `RootKinds` flags to generate the root as a method instead of a property, adjust its visibility (`Public`, `Internal`, `Private`), or make it `Static` or `Partial`.
$h=A private partial root is useful when you want to wrap the generated code in your own hand-written member, as the `PaymentService` property does here.
$h=The kinds combine with `|`, so one composition can expose the same contract through several roots of different shapes.
$f=Use kinds at the boundary of a library or an application: keep the public API to `Public | Property` roots, mark roots that only tests or internals need as `Internal`, and hide a root behind `Private | Partial` when a hand-written member must add validation, caching or a friendlier signature - that is what the `PaymentService` property does here.
$f=A root becomes a method on its own as soon as it declares root arguments or generic type arguments, so `Method` is needed only for a parameterless method.
$f=A `Partial` root is declared by the generator only: you must supply the matching partial member, otherwise the composition does not compile.
$f=A `Static` root cannot reach instance members of the composition and gets its own per-resolve lock instead of the instance-wide one, so keep static roots free of composition state.
$f=Two roots that would produce the same member name are reported as `DIE005`; keep names and name templates distinct when you combine kinds.
$f=Prefer the default `Public | Property` spelling unless a consumer needs otherwise, and add `Light` for cheap, frequently resolved roots ([Light roots](light-roots.md)).
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