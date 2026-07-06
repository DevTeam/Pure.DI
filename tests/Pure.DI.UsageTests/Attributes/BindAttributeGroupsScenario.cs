/*
$v=true
$p=21
$d=Bind attribute groups
$sa=Bind metadata merge
$h=Shows how separate `BindAttribute` groups let one implementation participate in several independent bindings.
$f=>[!NOTE]
$f=>Attributes inside one square-bracket group are merged into one binding. To create several bindings for the same implementation type, place `Bind` attributes in separate square-bracket groups.
$f=This is useful when one adapter has several roles: for example, it can be part of a normal processing pipeline and also be exposed through a tagged operational pipeline with another lifetime or contract.
$f=In this scenario, all bindings are declared with attributes, and the setup keeps only composition roots.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedPositionalProperty.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Attributes.BindAttributeGroupsScenario;

using System.Collections.Immutable;
using System.Linq;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Immutable;
//# using System.Linq;
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
            // Composition roots
            .Root<IOrderCheckout>("Checkout")
            .Root<IFailedPaymentReporter>("FailedPaymentReporter")
            .Root<IPaymentAuditSink>("AuditSink", "operations");

        var composition = new Composition();
        var checkout = composition.Checkout;
        var failedPaymentReporter = composition.FailedPaymentReporter;

        checkout.Processors.Length.ShouldBe(2);
        checkout.Processors.OfType<CardPaymentGateway>().Count().ShouldBe(1);

        var checkoutAudit = checkout.Processors.OfType<PaymentAuditAdapter>().Single();
        checkoutAudit.ShouldBeSameAs(composition.Checkout.Processors.OfType<PaymentAuditAdapter>().Single());

        failedPaymentReporter.AuditSinks.Length.ShouldBe(1);
        failedPaymentReporter.AuditSinks[0].ShouldBeOfType<PaymentAuditAdapter>();
        failedPaymentReporter.AuditSinks[0].ShouldNotBeSameAs(composition.AuditSink);
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IPaymentProcessor
{
    void Process(Payment payment);
}

interface IPaymentAuditSink
{
    void Record(Payment payment);
}

[Bind(typeof(IPaymentProcessor))]
class CardPaymentGateway : IPaymentProcessor
{
    public void Process(Payment payment)
    {
        // Calling an external card payment provider...
    }
}

interface IEventStore
{
    void Append(Payment payment);
}

[Bind(typeof(IEventStore), Lifetime.Singleton)]
class EventStore : IEventStore
{
    public void Append(Payment payment)
    {
        // Persisting an audit event...
    }
}

interface IOrderCheckout
{
    ImmutableArray<IPaymentProcessor> Processors { get; }
}

[Bind(typeof(IOrderCheckout))]
class OrderCheckout(IEnumerable<IPaymentProcessor> processors) : IOrderCheckout
{
    public ImmutableArray<IPaymentProcessor> Processors { get; } = [..processors];
}

interface IFailedPaymentReporter
{
    ImmutableArray<IPaymentAuditSink> AuditSinks { get; }
}

[Bind(typeof(IFailedPaymentReporter))]
class FailedPaymentReporter(
    [Tag("operations")] IEnumerable<IPaymentAuditSink> auditSinks)
    : IFailedPaymentReporter
{
    public ImmutableArray<IPaymentAuditSink> AuditSinks { get; } = [..auditSinks];
}

// Binding group #1:
// add this adapter to the main checkout pipeline as a tagged payment processor.
[Bind(typeof(IPaymentProcessor), Lifetime.Singleton, "audit")]

// Binding group #2:
// expose the same adapter as an operational audit sink with its own lifetime.
[Bind(typeof(IPaymentAuditSink), Lifetime.Transient, "operations")]
class PaymentAuditAdapter(IEventStore eventStore) :
    IPaymentProcessor,
    IPaymentAuditSink
{
    public void Process(Payment payment) => Record(payment);

    public void Record(Payment payment) => eventStore.Append(payment);
}

record Payment(decimal Amount);
// }
