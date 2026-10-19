/*
$v=true
$p=14
$d=Tag Unique
$sa=Tags
$h=`Tag.Unique` is useful to register a binding with a unique tag. It will not be available through the composition root or `Resolve` methods directly, but can be injected in compositions as some kind of enumeration.
$h=Each `Tag.Unique` binding gets its own tag, so any number of implementations of one contract can coexist and be collected into `IEnumerable<T>`, an array or an `ImmutableArray<T>`.
$f=Use this to aggregate several implementations without exposing each one as a direct root.
$f=This is how satellites are registered in a real project - trace sources, loggers, processors, validators - and then injected as a collection into the one component that orchestrates them.
$f=Limitations: unique-tag bindings are intentionally hidden from direct resolve; document this to avoid confusion in integration code.
$f=A unique binding that nothing collects is reported as `DIW003`, which is a useful signal that an implementation was registered but never reached.
$f=Prefer [Enumerable](enumerable.md) or `Tag.Type` when the consumer must address one specific implementation, because a unique tag cannot be named by the consumer at all.
$f=See also: [Tags](tags.md), [Enumerable](enumerable.md).
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
        // Disable Resolve methods to keep the public API minimal
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
