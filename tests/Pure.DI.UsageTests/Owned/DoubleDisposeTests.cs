// Owned<T> behaviour comparison: double disposal is idempotent.
//
// Disposing the same Owned<T> more than once - via Dispose(), DisposeAsync(), or
// a mix of both - must dispose the underlying instance exactly once and must not
// throw.
//
// Reference implementation: Autofac.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.DoubleDispose;

public class DoubleDisposeTests
{
    [Fact]
    public void Sync_dispose_twice_disposes_once_and_does_not_throw()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IResource> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IResource>>();
            IResource resource = owned.Value;

            owned.Dispose();
            owned.Dispose();

            resource.DisposeCount.ShouldBe(1);
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IResource> owned = composition.Root;
            IResource resource = owned.Value;

            owned.Dispose();
            owned.Dispose();

            resource.DisposeCount.ShouldBe(1);
        }
    }

    [Fact]
    public async Task Async_dispose_twice_disposes_once_and_does_not_throw()
    {
#if AUTOFAC_REFERENCE
        {
            IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IResource> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IResource>>();
            IResource resource = owned.Value;

            await owned.DisposeAsync();
            await owned.DisposeAsync();

            resource.DisposeCount.ShouldBe(1);
            await container.DisposeAsync();
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IResource> owned = composition.Root;
            IResource resource = owned.Value;

            await owned.DisposeAsync();
            await owned.DisposeAsync();

            resource.DisposeCount.ShouldBe(1);
        }
    }

    [Fact]
    public async Task Sync_dispose_then_async_dispose_disposes_once()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IResource> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IResource>>();
            IResource resource = owned.Value;

            owned.Dispose();
            await owned.DisposeAsync();

            resource.DisposeCount.ShouldBe(1);
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IResource> owned = composition.Root;
            IResource resource = owned.Value;

            owned.Dispose();
            await owned.DisposeAsync();

            resource.DisposeCount.ShouldBe(1);
        }
    }

#if AUTOFAC_REFERENCE
    private static IContainer BuildAutofac()
    {
        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterType<Resource>().As<IResource>().InstancePerDependency();
        return builder.Build();
    }
#endif
}

interface IResource
{
    int DisposeCount { get; }
}

sealed class Resource : IResource, IDisposable, IAsyncDisposable
{
    public int DisposeCount { get; private set; }

    public void Dispose() => DisposeCount++;

    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        return default;
    }
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IResource>().To<Resource>()
            .Root<Owned<IResource>>("Root");
}
