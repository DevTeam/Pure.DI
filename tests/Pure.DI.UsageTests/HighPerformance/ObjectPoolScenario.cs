/*
$v=true
$p=HighPerformance:5
$d=Object pool
$sa=ArrayPool buffer
$sa=Factory
$h=Object pools help when a service is expensive to allocate but can be reset and reused safely. A pool keeps a small set of warm objects and gives each request a short-lived lease that returns the instance when disposed.
$h=Here a notification pipeline rents an `EmailTemplateRenderer` for every message. The renderer owns reusable buffers that are cleared before it goes back to the pool, so the hot path avoids repeatedly allocating the renderer and its internal state.
$f=This pattern fits serializers, encoders, template renderers, parsers, and compression helpers. It is not a replacement for regular DI lifetimes: only pool objects that have a clear reset rule, are not used concurrently while leased, and do not keep request-specific references after `Reset()`.
$f=The composition owns the pool as a singleton, while each root call creates a lightweight lease. That keeps reuse explicit and avoids leaking pooled instances outside the operation scope.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.HighPerformance.ObjectPoolScenario;

using Shouldly;
using Xunit;
using static Pure.DI.Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
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
            .Bind().As(Singleton).To<RendererPool>()
            .Bind().To<RendererLease>()
            .Bind<INotificationComposer>().To<NotificationComposer>()
            .Root<INotificationComposer>("Composer");

        var composition = new Composition();
        var composer1 = composition.Composer;
        using var composer2 = composition.Composer;

        composer1.Compose("Ada", "Ready").ShouldBe("Hello Ada, Ready");
        composer2.Compose("Linus", "Done").ShouldBe("Hello Linus, Done");

        composer1.Lease.Renderer.ShouldNotBe(composer2.Lease.Renderer);
// }
        composer1.Dispose();
        using var composer3 = composition.Composer;
        composer3.Lease.Renderer.ShouldBe(composer1.Lease.Renderer);
        composition.SaveClassDiagram();
    }
}

// {
interface INotificationComposer : IDisposable
{
    RendererLease Lease { get; }

    string Compose(string userName, string message);
}

sealed class NotificationComposer(RendererLease lease) : INotificationComposer
{
    public RendererLease Lease => lease;

    public string Compose(string userName, string message) =>
        lease.Renderer.Render(userName, message);

    public void Dispose() => lease.Dispose();
}

sealed class RendererLease(RendererPool pool) : IDisposable
{
    public EmailTemplateRenderer Renderer { get; } = pool.Rent();

    public void Dispose()
    {
        Renderer.Reset();
        pool.Return(Renderer);
    }
}

sealed class RendererPool
{
    private readonly Stack<EmailTemplateRenderer> _renderers = [];

    public EmailTemplateRenderer Rent() =>
        _renderers.Count > 0 ? _renderers.Pop() : new EmailTemplateRenderer();

    public void Return(EmailTemplateRenderer renderer) =>
        _renderers.Push(renderer);
}

sealed class EmailTemplateRenderer
{
    private readonly List<string> _parts = [];

    public string Render(string userName, string message)
    {
        _parts.Add("Hello ");
        _parts.Add(userName);
        _parts.Add(", ");
        _parts.Add(message);
        return string.Concat(_parts);
    }

    public void Reset() => _parts.Clear();
}
// }
