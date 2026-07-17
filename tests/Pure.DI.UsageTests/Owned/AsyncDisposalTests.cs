// Owned<T> behaviour comparison: asynchronous disposal.
//
//   * DisposeAsync() disposes an IAsyncDisposable instance;
//   * a SYNC Dispose() over an async-only instance (implements IAsyncDisposable
//     but NOT IDisposable) - Pure.DI runs DisposeAsync() and blocks on it;
//     Autofac's behaviour is captured as the reference;
//   * a MIXED instance (both IDisposable and IAsyncDisposable): the sync path
//     must call Dispose(), the async path must call DisposeAsync().
//
// Reference implementation: Autofac.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.AsyncDisposal;

public class AsyncDisposalTests
{
    [Fact]
    public async Task DisposeAsync_disposes_an_async_only_instance()
    {
#if AUTOFAC_REFERENCE
        {
            IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IAsyncOnly> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IAsyncOnly>>();
            IAsyncOnly service = owned.Value;

            service.AsyncDisposed.ShouldBeFalse();
            await owned.DisposeAsync();
            service.AsyncDisposed.ShouldBeTrue();

            await container.DisposeAsync();
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IAsyncOnly> owned = composition.AsyncOnlyRoot;
            IAsyncOnly service = owned.Value;

            service.AsyncDisposed.ShouldBeFalse();
            await owned.DisposeAsync();
            service.AsyncDisposed.ShouldBeTrue();
        }
    }

    [Fact]
    public void Sync_dispose_of_an_async_only_instance()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IAsyncOnly> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IAsyncOnly>>();
            IAsyncOnly service = owned.Value;

            Exception? thrown = Record.Exception(() => owned.Dispose());
            DescribeSyncOverAsync(thrown, service).ShouldBe(ExpectedSyncOverAsync);
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IAsyncOnly> owned = composition.AsyncOnlyRoot;
            IAsyncOnly service = owned.Value;

            Exception? thrown = Record.Exception(() => owned.Dispose());
            DescribeSyncOverAsync(thrown, service).ShouldBe(ExpectedSyncOverAsync);
        }
    }

    [Fact]
    public void Sync_dispose_of_a_mixed_instance_calls_Dispose_only()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IMixed> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IMixed>>();
            IMixed service = owned.Value;

            owned.Dispose();
            DescribeMixed(service).ShouldBe("sync=True; async=False");
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IMixed> owned = composition.MixedRoot;
            IMixed service = owned.Value;

            owned.Dispose();
            DescribeMixed(service).ShouldBe("sync=True; async=False");
        }
    }

    [Fact]
    public async Task Async_dispose_of_a_mixed_instance_calls_DisposeAsync_only()
    {
#if AUTOFAC_REFERENCE
        {
            IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IMixed> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IMixed>>();
            IMixed service = owned.Value;

            await owned.DisposeAsync();
            DescribeMixed(service).ShouldBe("sync=False; async=True");

            await container.DisposeAsync();
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IMixed> owned = composition.MixedRoot;
            IMixed service = owned.Value;

            await owned.DisposeAsync();
            DescribeMixed(service).ShouldBe("sync=False; async=True");
        }
    }

    // Autofac (reference): captured below after observing.
    private const string ExpectedSyncOverAsync = "threw=False; asyncDisposed=True";

    private static string DescribeSyncOverAsync(Exception? thrown, IAsyncOnly service) =>
        $"threw={thrown is not null}; asyncDisposed={service.AsyncDisposed}";

    private static string DescribeMixed(IMixed service) =>
        $"sync={service.SyncDisposed}; async={service.AsyncDisposed}";

#if AUTOFAC_REFERENCE
    private static IContainer BuildAutofac()
    {
        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterType<AsyncOnly>().As<IAsyncOnly>().InstancePerDependency();
        builder.RegisterType<Mixed>().As<IMixed>().InstancePerDependency();
        return builder.Build();
    }
#endif
}

interface IAsyncOnly
{
    bool AsyncDisposed { get; }
}

sealed class AsyncOnly : IAsyncOnly, IAsyncDisposable
{
    public bool AsyncDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        AsyncDisposed = true;
        return default;
    }
}

interface IMixed
{
    bool SyncDisposed { get; }

    bool AsyncDisposed { get; }
}

sealed class Mixed : IMixed, IDisposable, IAsyncDisposable
{
    public bool SyncDisposed { get; private set; }

    public bool AsyncDisposed { get; private set; }

    public void Dispose() => SyncDisposed = true;

    public ValueTask DisposeAsync()
    {
        AsyncDisposed = true;
        return default;
    }
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IAsyncOnly>().To<AsyncOnly>()
            .Bind<IMixed>().To<Mixed>()
            .Root<Owned<IAsyncOnly>>("AsyncOnlyRoot")
            .Root<Owned<IMixed>>("MixedRoot");
}
