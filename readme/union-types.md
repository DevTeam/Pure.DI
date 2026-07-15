#### Union types

A union types C# feature allows declaring a union type such as `union PaymentGateway(StripeGateway, BankGateway);`. Each case type is implicitly convertible to the union, and Pure.DI uses this conversion to treat a case implementation as an implementation of the union DI contract. Unlike an interface contract, the case types do not have to share any common abstraction — they may even be third-party SDK types you cannot change. The checkout service below depends only on the `PaymentGateway` union and pattern matches over its cases, while the composition decides which gateway is used and builds the whole dependency graph of the selected case.


```c#
using Shouldly;
using Pure.DI;

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
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 11.0](https://dotnet.microsoft.com/en-us/download/dotnet/11.0) or later installed
```bash
dotnet --list-sdk
```
- Create a net11.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to the NuGet packages
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
  - [Shouldly](https://www.nuget.org/packages/Shouldly)
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

Swapping the payment provider is a one-line change in the setup: `.Bind<PaymentGateway>().To<BankGateway>()` makes the same checkout root transfer money through the bank gateway together with its own dependencies.
If there is no explicit union binding and exactly one registered binding can be implicitly converted to the requested union, Pure.DI resolves the union through that single case automatically:
```c#
DI.Setup(nameof(Composition))
    .Bind<StripeGateway>().To<StripeGateway>()
    .Root<PaymentGateway>("Gateway");
```
When several case bindings are applicable to the same union contract and tag, Pure.DI reports error `DIE050` instead of picking a case arbitrarily. Bind the union contract explicitly, use distinct tags, or remove one of the candidate bindings.
Composition arguments and root arguments can also provide a case value. For example, `.Arg<StripeGateway>("gateway")` can satisfy a `PaymentGateway` dependency, while `.RootArg<StripeGateway>("gateway")` produces a root method that converts the supplied gateway on each call.
Generic unions are supported in both closed and generic roots. A setup such as `.Bind<Success<TT>>().To<Success<TT>>().Root<Result<TT>>("GetResult")` produces a generic composition root and applies the case-to-union conversion after substituting the root type argument.
Collections deliberately keep the normal Pure.DI multi-binding rules. Register case bindings with `Tag.Unique` and request `IEnumerable<PaymentGateway>` to receive every registered case converted to the union. A single `PaymentGateway` request with several matching cases remains ambiguous and reports `DIE050`.
The generated code stays statically typed: the case instance is converted to the union by the C# compiler at the injection site, so lifetimes of case bindings remain visible to lifetime validation.
This scenario requires the preview language version and .NET 11 Preview 5 or later, where the union runtime types are available.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
  public CheckoutService Checkout
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return new CheckoutService(new StripeGateway(new StripeApiClient()), new ReceiptService());
    }
  }
}
```

</details>

Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	ReceiptService --|> IReceiptService
	StripeGateway --|> PaymentGateway
	Composition ..> CheckoutService : CheckoutService Checkout
	StripeGateway *-- StripeApiClient : StripeApiClient
	CheckoutService *-- ReceiptService : IReceiptService
	CheckoutService *-- StripeGateway : PaymentGateway
	namespace Pure.DI.UsageTests.Basics.UnionTypesScenario {
		class CheckoutService {
				<<class>>
			+CheckoutService(PaymentGateway gateway, IReceiptService receipts)
		}
		class Composition {
		<<partial>>
		+CheckoutService Checkout
		}
		class IReceiptService {
			<<interface>>
		}
		class PaymentGateway {
			<<struct>>
		}
		class ReceiptService {
				<<class>>
			+ReceiptService()
		}
		class StripeApiClient {
				<<class>>
			+StripeApiClient()
		}
		class StripeGateway {
				<<class>>
			+StripeGateway(StripeApiClient api)
		}
	}
```

