/*
$v=true
$t=Basics
$p=34
$d=Multiple implementations
$sa=Tag Unique
$sa=Tag Type
$sa=Simplified lifetime-specific bindings
$h=Use `To<T1, T2, ...>()` to bind several implementations to the same contract in one call. Each type becomes a separate binding with the configured lifetime and tags. Up to 64 implementation types are supported.
$h=Use `Tag.Unique` when a collection should contain every implementation. Pure.DI assigns a distinct tag to each binding, so an array or `IEnumerable<T>` can collect them all.
$f=You can use `Tag.Type` instead of `Tag.Unique` to give each binding the implementation type as its tag. For example, `EmailChannel` gets the tag `typeof(EmailChannel)`, which can also be used to resolve that implementation individually.
$f=A regular shared tag follows the usual binding rules: later bindings for the same contract and tag override earlier ones. `Tag.Unique` keeps each implementation available to collection injection.
$f=See also: [Tag Type](tag-type.md), [Tag Unique](tag-unique.md), [Simplified lifetime-specific bindings](simplified-lifetime-specific-bindings.md).
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.UsageTests.Basics.MultipleImplementationsScenario;

using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // Each channel gets a separate binding with Singleton lifetime.
            .Bind<INotificationChannel>(Tag.Unique)
                .As(Lifetime.Singleton)
                .To<EmailChannel, SmsChannel, PushChannel>()
            .Root<INotificationChannel[]>("Channels");

        var composition = new Composition();
        var channels = composition.Channels;

        channels.Length.ShouldBe(3);
        channels[0].ShouldBeOfType<EmailChannel>();
        channels[1].ShouldBeOfType<SmsChannel>();
        channels[2].ShouldBeOfType<PushChannel>();

        var channelsAgain = composition.Channels;
        for (var index = 0; index < channels.Length; index++)
        {
            channelsAgain[index].ShouldBeSameAs(channels[index]);
        }
// }
        composition.SaveClassDiagram();
    }
}

// {
interface INotificationChannel;

class EmailChannel : INotificationChannel;

class SmsChannel : INotificationChannel;

class PushChannel : INotificationChannel;
// }
