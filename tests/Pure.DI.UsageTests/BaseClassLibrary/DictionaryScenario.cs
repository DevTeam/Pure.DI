/*
$v=true
$p=7
$d=Dictionary
$sa=Enumerable
$sa=Tags
$h=When a service needs to pick a dependency by key at runtime — for example, choosing a notification channel — inject an `IReadOnlyDictionary<TKey, TValue>`. Bind each entry as a `KeyValuePair<TKey, TValue>` with `Tag.Unique`, and Pure.DI collects all such pairs into the dictionary automatically.
$f=>[!NOTE]
$f=>Dictionary injection is useful when you need to access dependencies by keys, such as named or tagged implementations like notification channels.
$f=
$f=The dictionary is built from the `KeyValuePair<TKey, TValue>` bindings, not from the channel bindings themselves, so the key is whatever the factory puts into the pair. That makes the key a compile-time constant you choose instead of a tag string a consumer has to repeat.
$f=`Tag.Unique` marks each pair as a separate entry; without it two pairs of the same pair type would compete as bindings. Add a pair by adding one binding, and the dictionary in every consumer gains the key.
$f=Two pairs with the same `TKey` value collide when the dictionary is built, so keep keys unique by construction — an enum makes a duplicate visible at the declaration site.
$f=>[!TIP]
$f=>Prefer a dictionary when the consumer selects by key at run time, and plain collection injection when it must always run all of them.
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