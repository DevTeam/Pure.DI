// Owned<T> behaviour comparison: the Owned<T> root VALUE itself is a Singleton.
//
// Expected (reference: Autofac SingleInstance + Owned<T>):
//   * disposing the Owned<T> is a no-op for the shared singleton;
//   * the container/composition still owns the singleton and MUST dispose it
//     on its own disposal.
//
// This asserts, in particular, that the generated composition implements
// IDisposable and releases the singleton on exit - otherwise the singleton
// would leak (never disposed by anyone).

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.OwnedSingletonRoot;

public class OwnedSingletonRootTests
{
    [Fact]
    public void Owned_of_singleton_is_disposed_by_the_container_not_by_the_owned()
    {
#if AUTOFAC_REFERENCE
        {
            IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IService> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IService>>();
            IService service = owned.Value;

            service.IsDisposed.ShouldBeFalse();

            // Disposing the Owned<T> must NOT dispose the shared singleton.
            owned.Dispose();
            service.IsDisposed.ShouldBeFalse();

            // The container owns the singleton and disposes it on exit.
            container.Dispose();
            service.IsDisposed.ShouldBeTrue();
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IService> owned = composition.Root;
            IService service = owned.Value;

            service.IsDisposed.ShouldBeFalse();

            // Disposing the Owned<T> must NOT dispose the shared singleton.
            owned.Dispose();
            service.IsDisposed.ShouldBeFalse();

            // The composition owns the singleton and must dispose it on exit.
            IDisposable? disposableComposition = composition as IDisposable;
            disposableComposition.ShouldNotBeNull();
            disposableComposition.Dispose();
            service.IsDisposed.ShouldBeTrue();
        }
    }

    [Fact]
    public void The_same_singleton_instance_is_returned_across_owned_roots()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IService> owned1 =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IService>>();
            Autofac.Features.OwnedInstances.Owned<IService> owned2 =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IService>>();

            ReferenceEquals(owned1.Value, owned2.Value).ShouldBeTrue();
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IService> owned1 = composition.Root;
            Owned<IService> owned2 = composition.Root;

            ReferenceEquals(owned1.Value, owned2.Value).ShouldBeTrue();
        }
    }

#if AUTOFAC_REFERENCE
    private static IContainer BuildAutofac()
    {
        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterType<Service>().As<IService>().SingleInstance();
        return builder.Build();
    }
#endif
}

interface IService
{
    bool IsDisposed { get; }
}

sealed class Service : IService, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind().As(Lifetime.Singleton).To<Service>()
            .Root<Owned<IService>>("Root");
}
