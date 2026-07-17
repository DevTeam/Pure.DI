// Owned<T> behaviour comparison: a nested Owned<T> graph with MIXED lifetimes.
//
// A Singleton (IShared, disposable) is reachable from BOTH the outer graph and
// the nested inner Owned<T> graph. Each graph also has its own transient-scoped
// disposable local.
//
//   Owned<IOuter>
//     Outer  ── IShared (Singleton) ── shared with ...
//       │      IOuterLocal (transient)
//       └── Func<Owned<IInner>>
//              Inner ── IShared (same Singleton)
//                       IInnerLocal (transient)
//
// The interesting risk: Pure.DI reuses a single per-root accumulator across the
// outer and inner Owned<T> (proved in NestedOwnedTests). This test checks the
// Singleton is NOT swept into that accumulator - it must survive owned disposal
// and be disposed once, only when the composition/container is disposed.
//
// Reference implementation: Autofac (SingleInstance + Owned<T>).

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.NestedOwnedMixedLifetime;

public class NestedOwnedMixedLifetimeTests
{
    [Fact]
    public void The_singleton_is_the_same_instance_in_the_outer_and_inner_graphs()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IOuter> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IOuter>>();

            ReferenceEquals(owned.Value.Shared, owned.Value.Inner.Shared).ShouldBeTrue();
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IOuter> owned = composition.Root;

            ReferenceEquals(owned.Value.Shared, owned.Value.Inner.Shared).ShouldBeTrue();
        }
    }

    [Fact]
    public void Disposing_the_outer_owned_does_not_dispose_the_singleton()
    {
#if AUTOFAC_REFERENCE
        {
            IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IOuter> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IOuter>>();
            IShared shared = owned.Value.Shared;

            owned.Dispose();
            shared.DisposeCount.ShouldBe(0);

            container.Dispose();
            shared.DisposeCount.ShouldBe(1);
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IOuter> owned = composition.Root;
            IShared shared = owned.Value.Shared;

            owned.Dispose();
            shared.DisposeCount.ShouldBe(0);

            IDisposable? disposableComposition = composition as IDisposable;
            disposableComposition.ShouldNotBeNull();
            disposableComposition.Dispose();
            shared.DisposeCount.ShouldBe(1);
        }
    }

    [Fact]
    public void Disposing_the_inner_owned_does_not_dispose_the_singleton()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IOuter> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IOuter>>();
            IShared shared = owned.Value.Shared;

            owned.Value.DisposeInner();
            shared.DisposeCount.ShouldBe(0);
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IOuter> owned = composition.Root;
            IShared shared = owned.Value.Shared;

            owned.Value.DisposeInner();
            shared.DisposeCount.ShouldBe(0);
        }
    }

#if AUTOFAC_REFERENCE
    private static IContainer BuildAutofac()
    {
        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterType<Shared>().As<IShared>().SingleInstance();
        builder.RegisterType<InnerLocal>().As<IInnerLocal>().InstancePerDependency();
        builder.RegisterType<OuterLocal>().As<IOuterLocal>().InstancePerDependency();
        builder.RegisterType<Inner>().As<IInner>().InstancePerDependency();
        builder.RegisterType<AutofacOuter>().As<IOuter>().InstancePerDependency();
        return builder.Build();
    }
#endif
}

interface IShared
{
    int DisposeCount { get; }
}

sealed class Shared : IShared, IDisposable
{
    public int DisposeCount { get; private set; }

    public void Dispose() => DisposeCount++;
}

interface IInnerLocal
{
    bool IsDisposed { get; }
}

sealed class InnerLocal : IInnerLocal, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IOuterLocal
{
    bool IsDisposed { get; }
}

sealed class OuterLocal : IOuterLocal, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IInner
{
    IShared Shared { get; }

    IInnerLocal Local { get; }
}

sealed class Inner(IShared shared, IInnerLocal local) : IInner
{
    public IShared Shared { get; } = shared;

    public IInnerLocal Local { get; } = local;
}

interface IOuter
{
    IShared Shared { get; }

    IOuterLocal Local { get; }

    IInner Inner { get; }

    void DisposeInner();
}

sealed class Outer(Func<Owned<IInner>> innerFactory, IShared shared, IOuterLocal local) : IOuter
{
    private readonly Owned<IInner> innerOwned = innerFactory();

    public IShared Shared { get; } = shared;

    public IOuterLocal Local { get; } = local;

    public IInner Inner => innerOwned.Value;

    public void DisposeInner() => innerOwned.Dispose();
}

#if AUTOFAC_REFERENCE
sealed class AutofacOuter(
    Func<Autofac.Features.OwnedInstances.Owned<IInner>> innerFactory,
    IShared shared,
    IOuterLocal local) : IOuter
{
    private readonly Autofac.Features.OwnedInstances.Owned<IInner> innerOwned = innerFactory();

    public IShared Shared { get; } = shared;

    public IOuterLocal Local { get; } = local;

    public IInner Inner => innerOwned.Value;

    public void DisposeInner() => innerOwned.Dispose();
}
#endif

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind().As(Lifetime.Singleton).To<Shared>()
            .Bind().To<InnerLocal>()
            .Bind().To<OuterLocal>()
            .Bind().To<Inner>()
            .Bind().To<Outer>()
            .Root<Owned<IOuter>>("Root");
}
