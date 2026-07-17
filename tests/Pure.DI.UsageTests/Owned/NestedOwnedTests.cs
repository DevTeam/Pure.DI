// Owned<T> behaviour comparison: independence of a nested Owned<T>.
//
// A component that lives inside an OUTER Owned<T> graph creates and holds an
// INNER Owned<T> (via Func<Owned<T>>). The whole point of Owned<T> is that each
// one manages an independent lifetime, so disposing one must not touch the
// other's instances.
//
// Reference (Autofac 8.x): each Owned<T> is an independent lifetime scope.
//   * disposing the outer Owned<T> leaves the inner instance alive;
//   * disposing the inner Owned<T> leaves the outer graph (a sibling disposable)
//     alive.
//
// Pure.DI SHARES ONE accumulator (`Pure.DI.Owned`) across the whole root
// resolution (see the generated Root getter: a single `perBlockOwned` is reused
// by both the outer and the inner Owned<T>). As a result the two Owned<T> are
// not independent - disposing either disposes both graphs. The Pure.DI asserts
// below therefore fail, which is the point of this comparison.
//
// The nested-owned wrapper is framework specific (Pure.DI.Owned<T> vs Autofac's
// Owned<T>), so each side has its own outer type; both implement IOuter.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.NestedOwned;

public class NestedOwnedTests
{
    [Fact]
    public void Disposing_the_outer_owned_leaves_the_independently_owned_inner_alive()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IOuter> outer =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IOuter>>();
            IInner inner = outer.Value.Inner;

            inner.IsDisposed.ShouldBeFalse();
            outer.Dispose();
            inner.IsDisposed.ShouldBeFalse();
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IOuter> outer = composition.Root;
            IInner inner = outer.Value.Inner;

            inner.IsDisposed.ShouldBeFalse();
            outer.Dispose();
            inner.IsDisposed.ShouldBeFalse();
        }
    }

    [Fact]
    public void Disposing_the_inner_owned_leaves_the_outer_graph_alive()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IOuter> outer =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IOuter>>();
            ISibling sibling = outer.Value.Sibling;

            sibling.IsDisposed.ShouldBeFalse();
            outer.Value.DisposeInner();
            sibling.IsDisposed.ShouldBeFalse();
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IOuter> outer = composition.Root;
            ISibling sibling = outer.Value.Sibling;

            sibling.IsDisposed.ShouldBeFalse();
            outer.Value.DisposeInner();
            sibling.IsDisposed.ShouldBeFalse();
        }
    }

    [Fact]
    public void Disposing_the_inner_owned_disposes_the_inner_instance()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IOuter> outer =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IOuter>>();
            IInner inner = outer.Value.Inner;

            outer.Value.DisposeInner();
            inner.IsDisposed.ShouldBeTrue();
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IOuter> outer = composition.Root;
            IInner inner = outer.Value.Inner;

            outer.Value.DisposeInner();
            inner.IsDisposed.ShouldBeTrue();
        }
    }

#if AUTOFAC_REFERENCE
    private static IContainer BuildAutofac()
    {
        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterType<Inner>().As<IInner>().InstancePerDependency();
        builder.RegisterType<Sibling>().As<ISibling>().InstancePerDependency();
        builder.RegisterType<AutofacOuter>().As<IOuter>().InstancePerDependency();
        return builder.Build();
    }
#endif
}

interface IInner
{
    bool IsDisposed { get; }
}

sealed class Inner : IInner, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

// A disposable that belongs to the OUTER graph only (never to the inner Owned).
interface ISibling
{
    bool IsDisposed { get; }
}

sealed class Sibling : ISibling, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IOuter
{
    IInner Inner { get; }

    ISibling Sibling { get; }

    void DisposeInner();
}

// Pure.DI outer: nests Pure.DI.Owned<IInner> obtained through Func<Owned<IInner>>.
sealed class Outer(Func<Owned<IInner>> innerFactory, ISibling sibling) : IOuter
{
    private readonly Owned<IInner> innerOwned = innerFactory();

    public IInner Inner => innerOwned.Value;

    public ISibling Sibling { get; } = sibling;

    public void DisposeInner() => innerOwned.Dispose();
}

#if AUTOFAC_REFERENCE
// Autofac outer: nests Autofac's own Owned<IInner>.
sealed class AutofacOuter(
    Func<Autofac.Features.OwnedInstances.Owned<IInner>> innerFactory,
    ISibling sibling) : IOuter
{
    private readonly Autofac.Features.OwnedInstances.Owned<IInner> innerOwned = innerFactory();

    public IInner Inner => innerOwned.Value;

    public ISibling Sibling { get; } = sibling;

    public void DisposeInner() => innerOwned.Dispose();
}
#endif

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind().To<Inner>()
            .Bind().To<Sibling>()
            .Bind().To<Outer>()
            .Root<Owned<IOuter>>("Root");
}
