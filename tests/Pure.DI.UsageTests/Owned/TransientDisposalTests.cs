// Owned<T> behaviour comparison: transient dependency disposal.
//
// Reference implementation: Autofac's Owned<T> (Autofac.Features.OwnedInstances).
// Every test asserts BOTH containers against the SAME hard-coded expected value
// that was deduced by observing Autofac. The Autofac side lives behind
// #if AUTOFAC_REFERENCE and is removed before the PR; the Pure.DI asserts and
// their literals stay as the record of the expected (Autofac) behaviour.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.TransientDisposal;

public class TransientDisposalTests
{
    [Fact]
    public void Disposing_owned_disposes_the_transient_dependency()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IService> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IService>>();
            IDependency dependency = owned.Value.Dependency;

            dependency.IsDisposed.ShouldBeFalse();
            owned.Dispose();
            dependency.IsDisposed.ShouldBeTrue();
        }
#endif
        {
            // The composition itself owns nothing disposable here (everything is
            // owned by the Owned<T> root), so it does not implement IDisposable.
            Composition composition = new Composition();
            Owned<IService> owned = composition.Root;
            IDependency dependency = owned.Value.Dependency;

            dependency.IsDisposed.ShouldBeFalse();
            owned.Dispose();
            dependency.IsDisposed.ShouldBeTrue();
        }
    }

    [Fact]
    public void Disposing_owned_disposes_a_transient_service_that_is_itself_disposable()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IService> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IService>>();
            IService service = owned.Value;

            service.IsDisposed.ShouldBeFalse();
            owned.Dispose();
            service.IsDisposed.ShouldBeTrue();
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IService> owned = composition.Root;
            IService service = owned.Value;

            service.IsDisposed.ShouldBeFalse();
            owned.Dispose();
            service.IsDisposed.ShouldBeTrue();
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

    bool IsDisposed { get; }
}

sealed class Service(IDependency dependency) : IService, IDisposable
{
    public IDependency Dependency { get; } = dependency;

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<Owned<IService>>("Root");
}
