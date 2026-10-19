/*
$v=true
$p=3
$d=Enumerable
$sa=Array
$sa=Enumerable generics
$sa=Async Enumerable
$h=Specifying `IEnumerable<T>` as the injection type lets you inject instances of all bindings that implement type `T` in a lazy fashion - the instances will be provided one by one, in order corresponding to the sequence of bindings.
$f=>[!NOTE]
$f=>IEnumerable<T> provides lazy evaluation, making it efficient for scenarios where you may not need to enumerate all instances.
$f=
$f=The sequence is produced by generated code and yields the bindings in declaration order, so `EmailSender` comes before the tagged `SmsSender`. In the scenario the consumer materializes it once into an `ImmutableArray<IMessageSender>`, because iterating a lazily created sequence twice would create transient senders twice.
$f=A collection parameter is not a `Singleton`: with transient bindings every enumeration yields new instances, which is fine for stateless handlers and wrong for anything that accumulates state. Enumerate once at construction, or make the elements `Singleton`.
$f=Adding a binding of the same contract silently changes what every collection consumer sees, even a consumer in another assembly compiled against the composition. Cover the expected order and count with a test.
$f=>[!TIP]
$f=>Prefer `IEnumerable<T>` for a pipeline that may skip elements, and an array or `ImmutableArray<T>` when the elements are read more than once.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.UsageTests.BCL.EnumerableScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Immutable;
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
            .Bind<IMessageSender>().To<EmailSender>()
            .Bind<IMessageSender>("sms").To<SmsSender>()
            .Bind<INotificationService>().To<NotificationService>()

            // Composition root
            .Root<INotificationService>("NotificationService");

        var composition = new Composition();
        var notificationService = composition.NotificationService;
        notificationService.Senders.Length.ShouldBe(2);
        notificationService.Senders[0].ShouldBeOfType<EmailSender>();
        notificationService.Senders[1].ShouldBeOfType<SmsSender>();

        notificationService.Notify("Hello World");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IMessageSender
{
    void Send(string message);
}

class EmailSender : IMessageSender
{
    public void Send(string message)
    {
        // Sending email...
    }
}

class SmsSender : IMessageSender
{
    public void Send(string message)
    {
        // Sending SMS...
    }
}

interface INotificationService
{
    ImmutableArray<IMessageSender> Senders { get; }

    void Notify(string message);
}

class NotificationService(IEnumerable<IMessageSender> senders) : INotificationService
{
    public ImmutableArray<IMessageSender> Senders { get; }
        = [..senders];

    public void Notify(string message)
    {
        foreach (var sender in Senders)
        {
            sender.Send(message);
        }
    }
}
// }