/*
$v=true
$p=3
$d=Dictionary
$h=Demonstrates dictionary injection using IReadOnlyDictionary<TKey, TValue>, allowing key-value pair collection injection.
$f=>[!NOTE]
$f=>Dictionary injection is useful when you need to access dependencies by keys, such as named or tagged implementations like notification channels.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable LocalizableElement
// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageTests.BCL.DictionaryScenario;

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
            .Bind(Tag.Unique).To((EmailChannel chanel) => new KeyValuePair<Channel, INotificationChannel>(Channel.Email, chanel))
            .Bind(Tag.Unique).To((SmsChannel chanel) => new KeyValuePair<Channel, INotificationChannel>(Channel.Sms, chanel))
            .Bind(Tag.Unique).To((PushChannel chanel) => new KeyValuePair<Channel, INotificationChannel>(Channel.Push, chanel))
            .Bind<INotificationService>().To<NotificationService>()

            // Composition root
            .Root<INotificationService>("NotificationService");

        var composition = new Composition();
        var notificationService = composition.NotificationService;

        // Verify that all notification channels are injected into the dictionary
        notificationService.Channels.Count.ShouldBe(3);
        notificationService.Channels[Channel.Email].ShouldBeOfType<EmailChannel>();
        notificationService.Channels[Channel.Sms].ShouldBeOfType<SmsChannel>();
        notificationService.Channels[Channel.Push].ShouldBeOfType<PushChannel>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface INotificationChannel
{
    void Send(string message);
}

class EmailChannel : INotificationChannel
{
    public void Send(string message) => Console.WriteLine($"Email: {message}");
}

class SmsChannel : INotificationChannel
{
    public void Send(string message) => Console.WriteLine($"SMS: {message}");
}

class PushChannel : INotificationChannel
{
    public void Send(string message) => Console.WriteLine($"Push: {message}");
}

enum Channel { Email, Sms, Push }

interface INotificationService
{
    IReadOnlyDictionary<Channel, INotificationChannel> Channels { get; }
}

class NotificationService(IReadOnlyDictionary<Channel, INotificationChannel> channels) : INotificationService
{
    public IReadOnlyDictionary<Channel, INotificationChannel> Channels { get; } = channels;
}
// }