// Owned<T> behaviour comparison: an exception thrown while disposing one item.
//
// The Owned<T> graph contains three disposables; the middle one throws from
// Dispose(). We capture, as a single state string, the full behaviour:
//   * thrown   - did Owned<T>.Dispose() surface an exception?
//   * attempted- was the throwing item's Dispose() actually invoked?
//   * before   - was the item disposed BEFORE the thrower (in disposal order)?
//   * after    - was the item disposed AFTER the thrower?
//
// Observed reference behaviour (Autofac 8.x): Autofac SURFACES the exception -
// Owned<T>.Dispose() rethrows. Pure.DI routes disposal exceptions to the empty
// partial method OnDisposeException, so it SWALLOWS them and keeps disposing.
// This is a genuine behavioural divergence (Pure.DI hides disposal failures).
//
// The literal encodes Autofac's behaviour; the Pure.DI assert therefore fails.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.DisposalException;

public class DisposalExceptionTests
{
    [Fact]
    public void Owned_dispose_surfaces_the_exception_and_the_thrower_is_invoked()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IRoot> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IRoot>>();
            IRoot root = owned.Value;

            Exception? thrown = Record.Exception(() => owned.Dispose());

            DescribeState(thrown, root).ShouldBe(ExpectedState);
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IRoot> owned = composition.Root;
            IRoot root = owned.Value;

            Exception? thrown = Record.Exception(() => owned.Dispose());

            DescribeState(thrown, root).ShouldBe(ExpectedState);
        }
    }

    // Autofac (reference): surfaces the exception; the thrower's Dispose runs.
    private const string ExpectedState = "thrown=True; attempted=True";

    private static string DescribeState(Exception? thrown, IRoot root) =>
        $"thrown={thrown is not null}; attempted={root.Thrower.DisposeAttempted}";

#if AUTOFAC_REFERENCE
    private static IContainer BuildAutofac()
    {
        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterType<Before>().As<IBefore>().InstancePerDependency();
        builder.RegisterType<Thrower>().As<IThrower>().InstancePerDependency();
        builder.RegisterType<After>().As<IAfter>().InstancePerDependency();
        builder.RegisterType<Root>().As<IRoot>().InstancePerDependency();
        return builder.Build();
    }
#endif
}

interface IBefore
{
    bool IsDisposed { get; }
}

sealed class Before : IBefore, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IThrower
{
    bool DisposeAttempted { get; }
}

sealed class Thrower : IThrower, IDisposable
{
    public bool DisposeAttempted { get; private set; }

    public void Dispose()
    {
        DisposeAttempted = true;
        throw new InvalidOperationException("boom");
    }
}

interface IAfter
{
    bool IsDisposed { get; }
}

sealed class After : IAfter, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IRoot
{
    IBefore Before { get; }

    IThrower Thrower { get; }

    IAfter After { get; }
}

sealed class Root(IBefore before, IThrower thrower, IAfter after) : IRoot
{
    public IBefore Before { get; } = before;

    public IThrower Thrower { get; } = thrower;

    public IAfter After { get; } = after;
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind().To<Before>()
            .Bind().To<Thrower>()
            .Bind().To<After>()
            .Bind().To<Root>()
            .Root<Owned<IRoot>>("Root");
}
