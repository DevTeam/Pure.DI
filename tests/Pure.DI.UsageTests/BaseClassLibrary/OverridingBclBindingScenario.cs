/*
$v=true
$p=16
$d=Overriding the BCL binding
$sa=Default BCL bindings
$h=At any time, the default binding to the BCL type can be changed to your own: a hand-written binding for a collection type replaces the one the generator creates from the individual bindings.
$f=>[!NOTE]
$f=>Overriding BCL bindings allows you to provide custom implementations for standard types, enabling specialized behavior for your application.
$f=
$f=A collection binding wins because it is more specific than the generated collection: the array here is built by the factory, so it decides the order, the length and even repeating one sender twice. That is how a pipeline gets an explicit sequence that individual bindings cannot express.
$f=The override hides every individual binding of the same contract from that collection. If both are needed, keep the explicit array binding for the ordered pipeline and use a tagged collection parameter for the discovered set.
$f=>[!TIP]
$f=>Prefer overriding a BCL collection only when order or duplication must be fixed by hand; otherwise let the generator collect the bindings, so adding one cannot be forgotten.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.BCL.OverridingBclBindingScenario;

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
            .Bind<IMessageSender[]>().To<IMessageSender[]>(() =>
                [new EmailSender(), new SmsSender(), new EmailSender()]
            )
            .Bind<INotificationService>().To<NotificationService>()

            // Composition root
            .Root<INotificationService>("NotificationService");

        var composition = new Composition();
        var notificationService = composition.NotificationService;
        notificationService.Senders.Length.ShouldBe(3);
        notificationService.Senders[0].ShouldBeOfType<EmailSender>();
        notificationService.Senders[1].ShouldBeOfType<SmsSender>();
        notificationService.Senders[2].ShouldBeOfType<EmailSender>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IMessageSender;

class EmailSender : IMessageSender;

class SmsSender : IMessageSender;

interface INotificationService
{
    IMessageSender[] Senders { get; }
}

class NotificationService(IMessageSender[] senders) : INotificationService
{
    public IMessageSender[] Senders { get; } = senders;
}
// }