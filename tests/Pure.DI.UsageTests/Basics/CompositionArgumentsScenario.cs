/*
$v=true
$t=QuickStart
$t=Basics
$p=9
$p=QuickStart:11
$d=Composition arguments
$sa=Root arguments
$sa=Overrides
$sa=Func with arguments
$h=Use composition arguments when the whole graph needs a value from the outside: configuration, a clock, an HTTP client, credentials. Declare them with `Arg<T>(argName)` (optionally with a tag) and use them like any other dependency; only arguments that the object graph actually uses become constructor parameters.
$h=This is the clean way to pass external runtime state without global statics, and it keeps the composition itself testable because the same setup works with different values.
$h=>[!NOTE]
$h=>Actually, composition arguments work like normal bindings. The difference is that they bind to the values of the arguments. These values will be injected wherever they are required.
$h=
$f=>[!NOTE]
$f=>Composition arguments provide a way to inject runtime values into the composition, making your DI configuration more flexible.
$r=Shouldly
$f=The argument name is validated as an identifier (`DIE006`) and becomes a named parameter of the composition constructor, so callers read `new Composition(connectionString: ...)` rather than a positional list.
$f=The value is fixed for the lifetime of that composition instance: everything resolved from it shares the same instance, so it behaves like an external immutable value and not like mutable state.
$f=Prefer a root argument when only one entry point needs the value; a composition argument that no graph uses is easy to leave behind after a refactor.
$f=Common pitfalls:
$f=- Using untagged primitive arguments where several values of the same type exist.
$f=- Treating composition arguments as mutable runtime state holders.
$f=See also: [Root arguments](root-arguments.md), [Tags](tags.md).
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.CompositionArgumentsScenario;

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
            .Bind<IBankGateway>().To<BankGateway>()
            .Bind<IPaymentProcessor>().To<PaymentProcessor>()

            // Composition root "PaymentService"
            .Root<IPaymentProcessor>("PaymentService")

            // Composition argument: Connection timeout (e.g., from config)
            .Arg<int>("timeoutSeconds")

            // Composition argument: API Token (using a tag to distinguish from other strings)
            .Arg<string>("authToken", "api token")

            // Composition argument: Bank gateway address
            .Arg<string>("gatewayUrl");

        // Create the composition, passing real settings from outside
        var composition = new Composition(
            timeoutSeconds: 30,
            authToken: "secret_token_123",
            gatewayUrl: "https://api.bank.com/v1");

        var paymentService = composition.PaymentService;

        paymentService.Token.ShouldBe("secret_token_123");
        paymentService.Gateway.Timeout.ShouldBe(30);
        paymentService.Gateway.Url.ShouldBe("https://api.bank.com/v1");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IBankGateway
{
    int Timeout { get; }

    string Url { get; }
}

// Simulation of a bank gateway client
class BankGateway(int timeoutSeconds, string gatewayUrl) : IBankGateway
{
    public int Timeout { get; } = timeoutSeconds;

    public string Url { get; } = gatewayUrl;
}

interface IPaymentProcessor
{
    string Token { get; }

    IBankGateway Gateway { get; }
}

// Payment processing service
class PaymentProcessor(
    // The tag allows specifying exactly which string to inject here
    [Tag("api token")] string token,
    IBankGateway gateway) : IPaymentProcessor
{
    public string Token { get; } = token;

    public IBankGateway Gateway { get; } = gateway;
}
// }
