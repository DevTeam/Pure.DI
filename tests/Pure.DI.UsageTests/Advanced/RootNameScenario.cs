/*
$v=true
$p=11
$d=Root Name
$h=`RootName` provides the name of the composition root being resolved. This property is useful for logging, diagnostics, or implementing root-specific behavior.
$h=Use this when infrastructure behavior should include root-level context (for example, logging prefixes).
$f=Limitations: root-name-dependent behavior couples logic to API naming; avoid it in domain services.
$f=See also: [Composition roots](composition-roots.md), [Root Type](root-type.md).
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable NotAccessedPositionalProperty.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.RootNameScenario;

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
// {
        var composition = new Composition();
        var orderService = composition.OrderService;
        orderService.Logger.Log("Processing order").ShouldContain("OrderService");

        var paymentService = composition.PaymentService;
        paymentService.Logger.Log("Processing payment").ShouldContain("PaymentService");
// }

        composition.SaveClassDiagram();
    }
}

// {
interface ILogger
{
    string Log(string message);
}

interface IOrderService
{
    ILogger Logger { get; }
}

interface IPaymentService
{
    ILogger Logger { get; }
}

class Logger(string rootName) : ILogger
{
    public string Log(string message) => $"[{rootName}] {message}";
}

class OrderService(ILogger logger) : IOrderService
{
    public ILogger Logger => logger;
}

class PaymentService(ILogger logger) : IPaymentService
{
    public ILogger Logger => logger;
}

partial class Composition
{
    private void Setup() =>
// }
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind().To(ctx => new Logger(ctx.RootName))
            .Bind().To<OrderService>()
            .Root<IOrderService>("OrderService")
            .Bind().To<PaymentService>()
            .Root<IPaymentService>("PaymentService");
}
// }
