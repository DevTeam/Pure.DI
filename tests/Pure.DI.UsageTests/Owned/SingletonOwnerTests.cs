// Owned<T> behaviour comparison: a long-lived Singleton that owns short-lived
// units of work through Func<Owned<T>>.
//
//   * when the owner explicitly releases (disposes) the Owned<T>, the resource
//     is disposed - both frameworks;
//   * when the owner creates an Owned<T> but never releases it (drops the
//     handle), what happens on container/composition disposal? Autofac tracks
//     the owned lifetime scope under the container and disposes it as a safety
//     net; Pure.DI transfers ownership fully, so a dropped Owned<T> is not
//     tracked by the composition. The literal encodes Autofac's behaviour.
//
// Reference implementation: Autofac.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.SingletonOwner;

public class SingletonOwnerTests
{
    [Fact]
    public void A_resource_the_owner_releases_is_disposed()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            IPool pool = container.Resolve<IPool>();

            IResource resource = pool.AcquireAndRelease();

            resource.IsDisposed.ShouldBeTrue();
        }
#endif
        {
            Composition composition = new Composition();
            IPool pool = composition.Pool;

            IResource resource = pool.AcquireAndRelease();

            resource.IsDisposed.ShouldBeTrue();
        }
    }

    [Fact]
    public void A_resource_the_owner_never_releases_is_disposed_on_container_disposal()
    {
#if AUTOFAC_REFERENCE
        {
            IContainer container = BuildAutofac();
            IPool pool = container.Resolve<IPool>();
            IResource resource = pool.AcquireAndForget();

            resource.IsDisposed.ShouldBeFalse();

            container.Dispose();
            resource.IsDisposed.ShouldBe(ForgottenResourceDisposedByContainer);
        }
#endif
        {
            Composition composition = new Composition();
            IPool pool = composition.Pool;
            IResource resource = pool.AcquireAndForget();

            resource.IsDisposed.ShouldBeFalse();

            (composition as IDisposable)?.Dispose();
            resource.IsDisposed.ShouldBe(ForgottenResourceDisposedByContainer);
        }
    }

    // Autofac (reference): a forgotten Owned<T> scope is still a child of the
    // container and is disposed when the container is disposed.
    private const bool ForgottenResourceDisposedByContainer = true;

#if AUTOFAC_REFERENCE
    private static IContainer BuildAutofac()
    {
        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterType<Resource>().As<IResource>().InstancePerDependency();
        builder.RegisterType<AutofacPool>().As<IPool>().SingleInstance();
        return builder.Build();
    }
#endif
}

interface IResource
{
    bool IsDisposed { get; }
}

sealed class Resource : IResource, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IPool
{
    IResource AcquireAndRelease();

    IResource AcquireAndForget();
}

sealed class Pool(Func<Owned<IResource>> resourceFactory) : IPool
{
    public IResource AcquireAndRelease()
    {
        using Owned<IResource> owned = resourceFactory();
        return owned.Value;
    }

    public IResource AcquireAndForget()
    {
        // Deliberately drops the Owned<T> handle without disposing it.
        Owned<IResource> owned = resourceFactory();
        return owned.Value;
    }
}

#if AUTOFAC_REFERENCE
sealed class AutofacPool(Func<Autofac.Features.OwnedInstances.Owned<IResource>> resourceFactory) : IPool
{
    public IResource AcquireAndRelease()
    {
        using Autofac.Features.OwnedInstances.Owned<IResource> owned = resourceFactory();
        return owned.Value;
    }

    public IResource AcquireAndForget()
    {
        Autofac.Features.OwnedInstances.Owned<IResource> owned = resourceFactory();
        return owned.Value;
    }
}
#endif

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind().To<Resource>()
            .Bind().As(Lifetime.Singleton).To<Pool>()
            .Root<IPool>("Pool");
}
