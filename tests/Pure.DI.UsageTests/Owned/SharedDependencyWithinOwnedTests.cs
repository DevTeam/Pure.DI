// Owned<T> behaviour comparison: a dependency shared by two consumers inside a
// single Owned<T> graph (a "diamond").
//
//        IRoot
//        /    \
//     ILeft   IRight
//        \    /
//        IShared   (disposable)
//
// Two lifetime variants:
//   * transient / InstancePerDependency  -> Left and Right get DIFFERENT shared
//     instances; each is disposed exactly once with the Owned<T>.
//   * PerResolve / InstancePerLifetimeScope -> Left and Right get the SAME
//     shared instance; it is disposed exactly once (not once per reference).
//
// Reference implementation: Autofac.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.SharedDependency;

public class SharedDependencyWithinOwnedTests
{
    [Fact]
    public void Transient_shared_dependency_is_two_instances_each_disposed_once()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac(sharedPerScope: false);
            Autofac.Features.OwnedInstances.Owned<IRoot> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IRoot>>();
            IShared left = owned.Value.Left.Shared;
            IShared right = owned.Value.Right.Shared;

            ReferenceEquals(left, right).ShouldBeFalse();

            owned.Dispose();
            left.DisposeCount.ShouldBe(1);
            right.DisposeCount.ShouldBe(1);
        }
#endif
        {
            TransientSharedComposition composition = new TransientSharedComposition();
            Owned<IRoot> owned = composition.Root;
            IShared left = owned.Value.Left.Shared;
            IShared right = owned.Value.Right.Shared;

            ReferenceEquals(left, right).ShouldBeFalse();

            owned.Dispose();
            left.DisposeCount.ShouldBe(1);
            right.DisposeCount.ShouldBe(1);
        }
    }

    [Fact]
    public void Scoped_shared_dependency_is_one_instance_disposed_once()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac(sharedPerScope: true);
            Autofac.Features.OwnedInstances.Owned<IRoot> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IRoot>>();
            IShared left = owned.Value.Left.Shared;
            IShared right = owned.Value.Right.Shared;

            ReferenceEquals(left, right).ShouldBeTrue();

            owned.Dispose();
            left.DisposeCount.ShouldBe(1);
        }
#endif
        {
            PerResolveSharedComposition composition = new PerResolveSharedComposition();
            Owned<IRoot> owned = composition.Root;
            IShared left = owned.Value.Left.Shared;
            IShared right = owned.Value.Right.Shared;

            ReferenceEquals(left, right).ShouldBeTrue();

            owned.Dispose();
            left.DisposeCount.ShouldBe(1);
        }
    }

#if AUTOFAC_REFERENCE
    private static IContainer BuildAutofac(bool sharedPerScope)
    {
        ContainerBuilder builder = new ContainerBuilder();
        if (sharedPerScope)
        {
            builder.RegisterType<Shared>().As<IShared>().InstancePerLifetimeScope();
        }
        else
        {
            builder.RegisterType<Shared>().As<IShared>().InstancePerDependency();
        }

        builder.RegisterType<Left>().As<ILeft>().InstancePerDependency();
        builder.RegisterType<Right>().As<IRight>().InstancePerDependency();
        builder.RegisterType<Root>().As<IRoot>().InstancePerDependency();
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

interface ILeft
{
    IShared Shared { get; }
}

sealed class Left(IShared shared) : ILeft
{
    public IShared Shared { get; } = shared;
}

interface IRight
{
    IShared Shared { get; }
}

sealed class Right(IShared shared) : IRight
{
    public IShared Shared { get; } = shared;
}

interface IRoot
{
    ILeft Left { get; }

    IRight Right { get; }
}

sealed class Root(ILeft left, IRight right) : IRoot
{
    public ILeft Left { get; } = left;

    public IRight Right { get; } = right;
}

partial class TransientSharedComposition
{
    private static void Setup() =>
        DI.Setup(nameof(TransientSharedComposition))
            .Bind().To<Shared>()
            .Bind().To<Left>()
            .Bind().To<Right>()
            .Bind().To<Root>()
            .Root<Owned<IRoot>>("Root");
}

partial class PerResolveSharedComposition
{
    private static void Setup() =>
        DI.Setup(nameof(PerResolveSharedComposition))
            .Bind().As(Lifetime.PerResolve).To<Shared>()
            .Bind().To<Left>()
            .Bind().To<Right>()
            .Bind().To<Root>()
            .Root<Owned<IRoot>>("Root");
}
