/*
$v=true
$p=4
$d=Type attribute
$h=Use the `Type` attribute to force a specific injected type, overriding the inferred type from the parameter or member. It fits when one class takes two dependencies of the same interface and each parameter must receive a different implementation, which the composition cannot tell apart by type alone.
$f=Because `[Type(typeof(...))]` names the implementation at the injection site, a type can say "this parameter needs a sender by e-mail, not just any sender" without adding a tag to the setup. Keep it for cases where the concrete type really is part of the contract, such as an adapter that must talk to one specific transport.
$f=The attribute overrides type inference, so the setup stops deciding which implementation arrives here: binding `IMessageSender` differently will not affect these parameters. It also couples the consumer to the implementation type, which erodes the abstraction if it spreads beyond a few intentional places.
$f=The `Type` attribute is part of the API, but you can define your own in any assembly or namespace.
$f=>[!TIP]
$f=>Prefer [tags](tag-attribute.md) when the choice should stay in the composition, and use `[Type]` when the consumer itself must pin one implementation.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Attributes.TypeAttributeScenario;

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
            .Bind().To<NotificationService>()

            // Composition root
            .Root<INotificationService>("NotificationService");

        var composition = new Composition();
        var notificationService = composition.NotificationService;
        notificationService.UserNotifier.ShouldBeOfType<EmailSender>();
        notificationService.AdminNotifier.ShouldBeOfType<SmsSender>();
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
    IMessageSender UserNotifier { get; }

    IMessageSender AdminNotifier { get; }
}

class NotificationService(
    // The [Type] attribute forces the injection of a specific type,
    // overriding the default resolution behavior.
    [Type(typeof(EmailSender))] IMessageSender userNotifier,
    [Type(typeof(SmsSender))] IMessageSender adminNotifier)
    : INotificationService
{
    public IMessageSender UserNotifier { get; } = userNotifier;

    public IMessageSender AdminNotifier { get; } = adminNotifier;
}
// }
