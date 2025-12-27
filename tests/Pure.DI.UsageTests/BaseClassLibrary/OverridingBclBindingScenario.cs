/*
$v=true
$p=100
$d=Overriding the BCL binding
$h=At any time, the default binding to the BCL type can be changed to your own:
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
        // This hint indicates to not generate methods such as Resolve
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