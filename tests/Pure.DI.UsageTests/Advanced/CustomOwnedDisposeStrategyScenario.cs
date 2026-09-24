/*
$v=true
$p=108
$d=Custom dispose strategy for Owned<T>
$sa=Tracing exceptions during Owned disposal
$sa=Custom dispose strategy
$h=`Owned<T>` calls `Dispose()` on every tracked resource when the owner is disposed. Some types — most notably WCF clients that implement `ICommunicationObject` — require a specific shutdown sequence (try `Close()`, fall back to `Abort()` on a timeout or when the channel is faulted). Implement the partial `OnDispose` / `OnDisposeAsync` hooks on the non-generic `Pure.DI.Owned` accumulator to perform the custom shutdown logic and set `skipDefaultDispose` to `true` to skip the default disposal call (or leave it `false` to let the owner dispose the instance as usual).
$f=>[!IMPORTANT]
$f=>The hook belongs to the non-generic `Pure.DI.Owned` accumulator used internally by every `Owned<T>`, so its partial implementation must be declared in the `Pure.DI` namespace.
$r=Shouldly
*/

// Demonstrates a custom shutdown sequence for the resources tracked by `Owned<T>`.
//
// `Owned<T>` disposes every resource in its graph when the owner is disposed, using the
// default `Dispose()` call. WCF-like clients instead require `Close(timeout)` with an
// `Abort()` fallback. That shutdown is plugged in through the partial
// `OnDispose` / `OnDisposeAsync` hooks of the non-generic `Pure.DI.Owned` accumulator:
// the hook releases the instance itself and sets `skipDefaultDispose` to `true`, which
// tells the accumulator not to call the default `Dispose()` on it.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable ArrangeNamespaceBody


namespace Pure.DI.UsageTests.Advanced.CustomOwnedDisposeStrategyScenario
{
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
// {
            // The root is Owned<IWcfClient>, so the resources it tracks are released
            // when the Owned<T> owner is disposed, not when the composition is.
            var composition = new Composition();
            var ownedClient = composition.Client;

            // Disposing the owner runs the custom hook instead of the default disposal.
            ownedClient.Dispose();

            // Only "Close" was recorded: the hook performed the graceful shutdown and
            // skipped the default Dispose(), which would have recorded "Default".
            ownedClient.Value.ShutdownSequence.ShouldBe(["Close"]);

            DI.Setup(nameof(Composition))
                .Bind<IWcfClient>().To<WcfClient>()
                .Root<Owned<IWcfClient>>("Client");
// }
        }
    }

// {
    // Minimal stand-in for a WCF client (ICommunicationObject): a graceful
    // Close(timeout) plus an Abort() fallback.
    public interface IWcfClient
    {
        // Records the shutdown calls in order so the test can assert which path was taken.
        IReadOnlyList<string> ShutdownSequence { get; }

        void Close(TimeSpan timeout);

        void Abort();
    }

    public sealed class WcfClient : IWcfClient, IDisposable
    {
        private readonly List<string> _sequence = [];

        public IReadOnlyList<string> ShutdownSequence => _sequence;

        public void Close(TimeSpan timeout) => _sequence.Add("Close");

        public void Abort() => _sequence.Add("Abort");

        // Only reached when the owner falls back to the default disposal strategy.
        public void Dispose() => _sequence.Add("Default");
    }
}
// }

// {
// The hook belongs to the non-generic Pure.DI.Owned accumulator shared by every Owned<T>,
// so the partial implementation must be declared in the Pure.DI namespace.
namespace Pure.DI
{
    internal sealed partial class Owned
    {
        // Called for each IDisposable tracked by any Owned<T> graph before the default disposal.
        // Set skipDefaultDispose to true after releasing the instance yourself, otherwise the
        // accumulator disposes it in the usual way.
        partial void OnDispose<T>(in T disposableInstance, ref bool skipDefaultDispose)
            where T : IDisposable
        {
            switch (disposableInstance)
            {
                // WCF-like clients need Close(timeout) with an Abort() fallback instead of Dispose().
                case UsageTests.Advanced.CustomOwnedDisposeStrategyScenario.IWcfClient wcfClient:
                    wcfClient.Close(TimeSpan.FromSeconds(10));
                    skipDefaultDispose = true;
                    break;

                // Any other resource: leave skipDefaultDispose as it is and let the
                // accumulator dispose it as usual.
            }
        }
    }
}
// }