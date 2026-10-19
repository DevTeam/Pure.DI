/*
$v=true
$p=HighPerformance:6
$d=Struct dependency
$sa=Span and ReadOnlySpan
$sa=Default Func with ReadOnlySpan
$h=Small immutable value-type services are useful on hot paths where the dependency represents a policy, a formatter, or a calculator with no identity and no shared mutable state. Pure.DI can compose those values directly, so consumers receive a strongly typed value without runtime lookup.
$h=This example uses a `readonly struct` shipping-price policy in a checkout quote calculator. The policy is cheap to copy, deterministic, and has no lifetime state, which makes it a good fit for value semantics.
$f=Prefer this pattern for tiny stateless rules and calculations. Do not turn large mutable objects into structs just to avoid allocation; copying a large struct can be more expensive than allocating one object. Keep value-type services small, immutable, and obvious.
$f=
$f=A policy, a formatter, a converter or a validator with no dependencies and no identity is the case: it is injected as a value, copied by the consumer, and never tracked or disposed. That also removes it from every lifetime question, because there is nothing to share.
$f=>[!NOTE]
$f=>Lifetimes do not apply to value types in a useful way: a stored struct is copied into each consumer, so a struct that mutates internal state does not share that state. Mark such types `readonly` and return results instead of mutating.
$f=
$f=Prefer a `readonly struct` for a pure function of its inputs, and a class when the dependency has state, dependencies of its own, or is large. See [Struct dependency](struct-dependency.md) for the contract, and [Default Func with ReadOnlySpan](default-func-with-readonlyspan.md) when the dependency is called with stack-only data.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
#pragma warning disable CA1822
namespace Pure.DI.UsageTests.HighPerformance.StructDependencyScenario;

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
            .Bind().To<ShippingPolicy>()
            .Bind<IQuoteCalculator>().To<QuoteCalculator>()
            .Root<IQuoteCalculator>("Calculator");

        var composition = new Composition();
        var calculator = composition.Calculator;

        calculator.GetTotal(new Cart(120m, 2.5m)).ShouldBe(125.99m);
// }
        composition.SaveClassDiagram();
    }
}

// {
readonly record struct Cart(decimal Subtotal, decimal WeightKg);

readonly struct ShippingPolicy
{
    public decimal Calculate(decimal weightKg) =>
        weightKg <= 1m ? 2.99m : 5.99m;
}

interface IQuoteCalculator
{
    decimal GetTotal(Cart cart);
}

sealed class QuoteCalculator(ShippingPolicy shippingPolicy) : IQuoteCalculator
{
    public decimal GetTotal(Cart cart) =>
        cart.Subtotal + shippingPolicy.Calculate(cart.WeightKg);
}
// }
