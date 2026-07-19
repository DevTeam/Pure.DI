// Owned<T> behaviour comparison: isolation between independent Owned<T> roots.
//
// Disposing one Owned<T> must not affect instances owned by another Owned<T>.
// Reference implementation: Autofac's Owned<T>.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.OwnedIsolation;

public class OwnedIsolationTests
{
    [Fact]
    public void Two_owned_roots_are_independent()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IService> owned1 =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IService>>();
            Autofac.Features.OwnedInstances.Owned<IService> owned2 =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IService>>();
            IDependency dependency1 = owned1.Value.Dependency;
            IDependency dependency2 = owned2.Value.Dependency;

            ReferenceEquals(dependency1, dependency2).ShouldBeFalse();

            owned1.Dispose();
            dependency1.IsDisposed.ShouldBeTrue();
            dependency2.IsDisposed.ShouldBeFalse();

            owned2.Dispose();
            dependency2.IsDisposed.ShouldBeTrue();
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IService> owned1 = composition.Root;
            Owned<IService> owned2 = composition.Root;
            IDependency dependency1 = owned1.Value.Dependency;
            IDependency dependency2 = owned2.Value.Dependency;

            ReferenceEquals(dependency1, dependency2).ShouldBeFalse();

            owned1.Dispose();
            dependency1.IsDisposed.ShouldBeTrue();
            dependency2.IsDisposed.ShouldBeFalse();

            owned2.Dispose();
            dependency2.IsDisposed.ShouldBeTrue();
        }
    }

#if AUTOFAC_REFERENCE
    private static IContainer BuildAutofac()
    {
        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterType<Dependency>().As<IDependency>().InstancePerDependency();
        builder.RegisterType<Service>().As<IService>().InstancePerDependency();
        return builder.Build();
    }
#endif
}

interface IDependency
{
    bool IsDisposed { get; }
}

sealed class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    IDependency Dependency { get; }
}

sealed class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<Owned<IService>>("Root");
}
