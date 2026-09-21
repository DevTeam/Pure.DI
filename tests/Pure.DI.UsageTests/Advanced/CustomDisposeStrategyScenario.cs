/*
$v=true
$p=101
$d=Custom dispose strategy
$sa=Tracking disposable instances per a composition root
$sa=Tracking owned disposable exceptions
$h=The composition calls `Dispose()` on tracked singleton and scoped instances. Some types — most notably WCF clients that implement `ICommunicationObject` — require a specific shutdown sequence (try `Close()`, fall back to `Abort()` on a timeout or when the channel is faulted). Implement the partial `OnDispose` / `OnDisposeAsync` methods on the composition, perform the custom shutdown logic and return `true` to skip the default disposal call (or `false` to let the composition dispose the instance as usual).
$f=>[!IMPORTANT]
$f=>`OnDispose` and `OnDisposeAsync` are partial methods. They are not invoked when not implemented, so there is zero overhead for compositions that do not need a custom disposal strategy.
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable InconsistentNaming

// ReSharper disable InvertIf
namespace Pure.DI.UsageTests.Advanced.CustomDisposeStrategyScenario;

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
        var composition = new Composition();
        var service = composition.Service;

        // Simulate using the service
        service.Open();

        // Dispose the composition - this triggers our custom dispose strategy
        composition.Dispose();

        // The custom disposal strategy ensured the channel was properly closed
        service.State.ShouldBe("Closed");
// }
        new Composition().SaveClassDiagram();
    }
}

// {
// Simulates a WCF-like client that requires Close-with-timeout and Abort fallback.
class WcfLikeChannel : IDisposable, IAsyncDisposable
{
    public string State { get; private set; } = "Created";

    public void Open() => State = "Opened";

    public void Close(TimeSpan timeout)
    {
        if (State == "Faulted")
        {
            throw new InvalidOperationException("Cannot close a faulted channel.");
        }

        State = "Closed";
    }

    public void Abort() => State = "Aborted";

    public void Dispose() => State = "Disposed";

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

interface IService
{
    string State { get; }

    void Open();
}

class Service(WcfLikeChannel channel) : IService
{
    public string State => channel.State;

    public void Open() => channel.Open();
}

partial class Composition
{
    static void Setup() =>
// }
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup()
            .Hint(Hint.OnDispose, "On")
            .Hint(Hint.OnDisposeAsync, "On")
            .Bind().As(Lifetime.Singleton).To<WcfLikeChannel>()
            .Bind().To<Service>()
            .Root<IService>("Service");

    // Custom dispose strategy: wrap each tracked WcfLikeChannel instance
    // in a "SafeClose" helper that performs Close(timeout) -> Abort() fallback.
    // Returns true to tell the composition to skip the default Dispose() / DisposeAsync()
    // call because the hook has already released the resource.
    private partial bool OnDispose<T>(in T disposableInstance)
        where T : IDisposable
    {
        if (disposableInstance is WcfLikeChannel channel)
        {
            SafeShutdown(channel);
            return true;
        }

        return false;
    }

    private partial ValueTask<bool> OnDisposeAsync<T>(in T asyncDisposableInstance)
        where T : IAsyncDisposable
    {
        if (asyncDisposableInstance is WcfLikeChannel channel)
        {
            SafeShutdown(channel);
            return new ValueTask<bool>(true);
        }

        return new ValueTask<bool>(false);
    }

    private static void SafeShutdown(WcfLikeChannel channel)
    {
        if (channel.State == "Opened")
        {
            try
            {
                channel.Close(TimeSpan.FromSeconds(10));
            }
            catch
            {
                channel.Abort();
            }
        }
        else
        {
            channel.Dispose();
        }
    }
}
// }
