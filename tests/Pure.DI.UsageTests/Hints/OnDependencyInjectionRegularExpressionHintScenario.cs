/*
$v=true
$p=2
$d=OnDependencyInjection regular expression hint
$sa=OnDependencyInjection wildcard hint
$h=Hints are used to fine-tune code generation. The `OnDependencyInjection` hint determines whether to generate partial `OnDependencyInjection` method to control of dependency injection.
$h=The generated method wraps the moment a value enters a consumer, which makes it the place for cross-cutting work that belongs to every injection: validation, logging, metrics, or swapping a value for a decorated one.
$h=In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// OnDependencyInjection = On`.
$f=The `OnDependencyInjectionContractTypeNameRegularExpression` hint helps identify the set of types that require injection control. You can use it to specify a regular expression to filter the full name of a type.
$f=The hook receives the value and returns the value to inject, so returning a different instance replaces the dependency transparently and returning `value` keeps it unchanged.
$f=It is called once per injection site in the object graph, not once per resolution step, and the `in` parameter avoids copying a struct dependency.
$f=The filter here is `(.*Gateway|int)$`, so `PaymentService` itself is not logged even though it is created inside the same graph - the hook follows what is injected, not what is constructed.
$f=Prefer this hook for observation and policy on the injection path; for measuring construction, use `OnNewInstance`, and for diagnostics only, keep the code behind `#if DEBUG`.
$f=For more hints, see [this](../README.md#setup-hints) page.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global

// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.UsageTests.Hints.OnDependencyInjectionRegularExpressionHintScenario;

using Shouldly;
using Xunit;
using static Hint;

// {
//# using Pure.DI;
//# using static Pure.DI.Hint;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        // OnDependencyInjection = On
        DI.Setup(nameof(Composition))
            // Filters types by regular expression to control which types trigger the OnDependencyInjection method.
            // In this case, we want to intercept the injection of any "Gateway" (like IPaymentGateway)
            // and integer configuration values.
            .Hint(OnDependencyInjectionContractTypeNameRegularExpression, "(.*Gateway|int)$")
            .RootArg<int>("maxAttempts")
            .Bind().To<PayPalGateway>()
            .Bind().To<PaymentService>()
            .Root<IPaymentService>("GetPaymentService");

        var log = new List<string>();
        var composition = new Composition(log);

        // Resolving the root service triggers the injection chain.
        // 1. int maxAttempts is injected into PayPalGateway.
        // 2. PayPalGateway is injected into PaymentService.
        // PaymentService itself is not logged because "IPaymentService" does not match the regex.
        var service = composition.GetPaymentService(3);

        log.ShouldBe([
            "Int32 injected",
            "PayPalGateway injected"
        ]);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IPaymentGateway;

record PayPalGateway(int MaxAttempts) : IPaymentGateway;

interface IPaymentService
{
    IPaymentGateway Gateway { get; }
}

class PaymentService(IPaymentGateway gateway) : IPaymentService
{
    public IPaymentGateway Gateway { get; } = gateway;
}

partial class Composition(List<string> log)
{
    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        // Logs the actual runtime type of the injected instance
        log.Add($"{value?.GetType().Name} injected");
        return value;
    }
}
// }