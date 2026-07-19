// Owned<T> behaviour comparison: a Singleton reached through an Owned<T>.
//
// Disposing the Owned<T> must NOT dispose a shared Singleton dependency - the
// singleton is owned by the container/composition and stays alive for other
// consumers until the container/composition itself is disposed.
// Reference implementation: Autofac's SingleInstance + Owned<T>.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.SingletonThroughOwned;

public class SingletonThroughOwnedTests
{
    [Fact]
    public void Disposing_owned_does_not_dispose_a_singleton_but_container_disposal_does()
    {
#if AUTOFAC_REFERENCE
        {
            IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IConsumer> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IConsumer>>();
            IConnection connection = owned.Value.Connection;

            connection.IsDisposed.ShouldBeFalse();

            // Disposing the Owned<T> releases ownership but not the singleton.
            owned.Dispose();
            connection.IsDisposed.ShouldBeFalse();

            // Disposing the container disposes the singleton.
            container.Dispose();
            connection.IsDisposed.ShouldBeTrue();
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IConsumer> owned = composition.Root;
            IConnection connection = owned.Value.Connection;

            connection.IsDisposed.ShouldBeFalse();

            // Disposing the Owned<T> releases ownership but not the singleton.
            owned.Dispose();
            connection.IsDisposed.ShouldBeFalse();

            // Disposing the composition disposes the singleton.
            composition.Dispose();
            connection.IsDisposed.ShouldBeTrue();
        }
    }

#if AUTOFAC_REFERENCE
    private static IContainer BuildAutofac()
    {
        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterType<Connection>().As<IConnection>().SingleInstance();
        builder.RegisterType<Consumer>().As<IConsumer>().InstancePerDependency();
        return builder.Build();
    }
#endif
}

interface IConnection
{
    bool IsDisposed { get; }
}

sealed class Connection : IConnection, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IConsumer
{
    IConnection Connection { get; }
}

sealed class Consumer(IConnection connection) : IConsumer
{
    public IConnection Connection { get; } = connection;
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind().As(Lifetime.Singleton).To<Connection>()
            .Bind().To<Consumer>()
            .Root<Owned<IConsumer>>("Root");
}
