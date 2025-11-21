/*
$v=true
$p=4
$d=Tag Unique
$h=`Tag.Unique` is useful to register a binding with a unique tag. It will not be available through the composition root or `Resolve` methods directly, but can be injected in compositions as some kind of enumeration.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedTypeParameter

namespace Pure.DI.UsageTests.Advanced.TagUniqueScenario;

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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind<INotificationChannel<TT>>(Tag.Unique).To<EmailChannel<TT>>()
            .Bind<INotificationChannel<TT>>(Tag.Unique).To<SmsChannel<TT>>()
            .Bind<INotificationService<TT>>().To<NotificationService<TT>>()

            // Composition root
            .Root<INotificationService<string>>("NotificationService");

        var composition = new Composition();
        var notificationService = composition.NotificationService;
        notificationService.Channels.Length.ShouldBe(2);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface INotificationChannel<T>;

class EmailChannel<T> : INotificationChannel<T>;

class SmsChannel<T> : INotificationChannel<T>;

interface INotificationService<T>
{
    ImmutableArray<INotificationChannel<T>> Channels { get; }
}

class NotificationService<T>(IEnumerable<INotificationChannel<T>> channels)
    : INotificationService<T>
{
    public ImmutableArray<INotificationChannel<T>> Channels { get; }
        = [..channels];
}
// }