/*
$v=true
$p=HighPerformance:15
$d=Factory without closure capture
$sa=Factory
$sa=Simplified factory
$h=Factory delegates often appear in hot object creation paths. Keep them deterministic and allocation-friendly by taking dependencies as factory parameters instead of capturing outer variables. Pure.DI can see those parameters as dependencies and generate the code that supplies them.
$h=The receipt formatter below needs a currency formatter and tax policy. Both are declared as lambda parameters, so the factory does not close over mutable setup-local state and the generated graph remains explicit.
$f=This is most useful when a factory adds a small construction decision or validates dependencies before creating an object. If the factory needs runtime values, prefer root arguments or `Func<TArg, TResult>` instead of capturing variables from the setup method.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1822
namespace Pure.DI.UsageTests.HighPerformance.FactoryWithoutClosureCaptureScenario;

using System.Globalization;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Globalization;
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
            .Bind().To<CurrencyFormatter>()
            .Bind().To<TaxPolicy>()
            .Bind<IReceiptFormatter>().To((
                CurrencyFormatter currency,
                TaxPolicy tax) => new ReceiptFormatter(currency, tax))
            .Root<IReceiptFormatter>("Formatter");

        var composition = new Composition();
        var formatter = composition.Formatter;

        formatter.Format(100m).ShouldBe("$120.00");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IReceiptFormatter
{
    string Format(decimal subtotal);
}

sealed class ReceiptFormatter(
    CurrencyFormatter currency,
    TaxPolicy tax)
    : IReceiptFormatter
{
    public string Format(decimal subtotal) =>
        currency.Format(tax.Apply(subtotal));
}

sealed class CurrencyFormatter
{
    public string Format(decimal value) =>
        $"${value.ToString("0.00", CultureInfo.InvariantCulture)}";
}

readonly struct TaxPolicy
{
    public decimal Apply(decimal subtotal) => subtotal * 1.20m;
}
// }
