/*
$v=true
$t=Basics
$p=110
$d=Union types
$n=11.0
$h=A union types C# feature allows declaring a union type such as `union PaymentGateway(StripeGateway, BankGateway);`. Each case type is implicitly convertible to the union, and Pure.DI uses this conversion to treat a case implementation as an implementation of the union DI contract. Unlike an interface contract, the case types do not have to share any common abstraction — they may even be third-party SDK types you cannot change. The checkout service below depends only on the `PaymentGateway` union and pattern matches over its cases, while the composition decides which gateway is used and builds the whole dependency graph of the selected case.
$f=Swapping the payment provider is a one-line change in the setup: `.Bind<PaymentGateway>().To<BankGateway>()` makes the same checkout root transfer money through the bank gateway together with its own dependencies.
$f=If there is no explicit union binding and exactly one registered binding can be implicitly converted to the requested union, Pure.DI resolves the union through that single case automatically:
$f=```c#
$f=DI.Setup(nameof(Composition))
$f=    .Bind<StripeGateway>().To<StripeGateway>()
$f=    .Root<PaymentGateway>("Gateway");
$f=```
$f=When several case bindings are applicable to the same union contract and tag, Pure.DI reports error `DIE050` instead of picking a case arbitrarily. Bind the union contract explicitly, use distinct tags, or remove one of the candidate bindings.
$f=Composition arguments and root arguments can also provide a case value. For example, `.Arg<StripeGateway>("gateway")` can satisfy a `PaymentGateway` dependency, while `.RootArg<StripeGateway>("gateway")` produces a root method that converts the supplied gateway on each call.
$f=Generic unions are supported in both closed and generic roots. A setup such as `.Bind<Success<TT>>().To<Success<TT>>().Root<Result<TT>>("GetResult")` produces a generic composition root and applies the case-to-union conversion after substituting the root type argument.
$f=Collections deliberately keep the normal Pure.DI multi-binding rules. Register case bindings with `Tag.Unique` and request `IEnumerable<PaymentGateway>` to receive every registered case converted to the union. A single `PaymentGateway` request with several matching cases remains ambiguous and reports `DIE050`.
$f=The generated code stays statically typed: the case instance is converted to the union by the C# compiler at the injection site, so lifetimes of case bindings remain visible to lifetime validation.
$f=This scenario requires the preview language version and .NET 11 Preview 5 or later, where the union runtime types are available.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local

namespace Pure.DI.UsageTests.Basics.UnionTypesScenario;

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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind<IReceiptService>().To<ReceiptService>()

            // The composition decides which payment gateway
            // stands behind the union contract
            .Bind<PaymentGateway>().To<StripeGateway>()

            // Composition root
            .Root<CheckoutService>("Checkout");

        var composition = new Composition();
        var checkout = composition.Checkout;
        checkout.Pay(4999).ShouldBe("Receipt: stripe/api charge 4999");
// }
        composition.SaveClassDiagram();
    }
}

// {
// The case types are independent classes, possibly from
// different vendor SDKs, and share no common interface.
// Each of them has its own dependencies that the composition
// builds behind the union contract.
class StripeApiClient
{
    public string Post(string request) => $"stripe/api {request}";
}

class StripeGateway(StripeApiClient api)
{
    public string Charge(int amountInCents) =>
        api.Post($"charge {amountInCents}");
}

class BankAccount
{
    public string Iban => "DE00 1234 5678";
}

class BankGateway(BankAccount account)
{
    public string Transfer(int amountInCents) =>
        $"bank transfer {amountInCents} from {account.Iban}";
}

// A union type: its case types are implicitly convertible
// to the union, and Pure.DI uses this conversion
// to satisfy the union contract
union PaymentGateway(StripeGateway, BankGateway);

interface IReceiptService
{
    string Print(string confirmation);
}

class ReceiptService : IReceiptService
{
    public string Print(string confirmation) => $"Receipt: {confirmation}";
}

// The service depends on the union contract instead of
// a specific gateway and handles each case explicitly
class CheckoutService(PaymentGateway gateway, IReceiptService receipts)
{
    public string Pay(int amountInCents)
    {
        var confirmation = gateway switch
        {
            StripeGateway stripe => stripe.Charge(amountInCents),
            BankGateway bank => bank.Transfer(amountInCents),
            _ => throw new InvalidOperationException("Unknown payment gateway.")
        };

        return receipts.Print(confirmation);
    }
}
// }
