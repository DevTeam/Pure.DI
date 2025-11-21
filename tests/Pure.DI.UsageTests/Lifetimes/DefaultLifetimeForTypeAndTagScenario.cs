/*
$v=true
$p=6
$d=Default lifetime for a type and a tag
$h=For example, if a certain lifetime is used more often than others, you can make it the default lifetime for a certain type:
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Lifetimes.DefaultLifetimeForTypeAndTagScenario;

using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
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
            // Real-world idea:
            // "Live" audio capture device should be shared (singleton),
            // while a regular (untagged) audio source can be created per session (transient).
            .DefaultLifetime<IAudioSource>(Singleton, "Live")

            // Tagged binding: "Live" audio capture (shared)
            .Bind("Live").To<LiveAudioSource>()

            // Untagged binding: some other source (new instance each time)
            .Bind().To<BufferedAudioSource>()

            // A playback session uses two sources:
            // - Live (shared, tagged)
            // - Buffered (transient, untagged)
            .Bind().To<PlaybackSession>()

            // Composition root
            .Root<IPlaybackSession>("PlaybackSession");

        var composition = new Composition();

        // Two independent sessions (transient root)
        var session1 = composition.PlaybackSession;
        var session2 = composition.PlaybackSession;

        session1.ShouldNotBe(session2);

        // Within a single session:
        // - Live source is tagged => default lifetime forces it to be shared (singleton)
        // - Buffered source is untagged => transient => always a new instance
        session1.LiveSource.ShouldNotBe(session1.BufferedSource);

        // Between sessions:
        // - Live source is a shared singleton (same instance)
        // - Buffered source is transient (different instances)
        session1.LiveSource.ShouldBe(session2.LiveSource);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IAudioSource;

// "Live" device: e.g., microphone/line-in capture.
class LiveAudioSource : IAudioSource;

// "Buffered" source: e.g., decoded audio chunks, per-session pipeline buffer.
class BufferedAudioSource : IAudioSource;

interface IPlaybackSession
{
    IAudioSource LiveSource { get; }

    IAudioSource BufferedSource { get; }
}

class PlaybackSession(
    // Tagged dependency: should be singleton because of DefaultLifetime<IAudioSource>(..., "Live")
    [Tag("Live")] IAudioSource liveSource,

    // Untagged dependency: transient by default
    IAudioSource bufferedSource)
    : IPlaybackSession
{
    public IAudioSource LiveSource { get; } = liveSource;

    public IAudioSource BufferedSource { get; } = bufferedSource;
}
// }