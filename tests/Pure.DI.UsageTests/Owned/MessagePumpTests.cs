// Owned<T> behaviour comparison: the canonical "message pump" unit-of-work loop
// from the Autofac docs.
//
//   class MessagePump(Func<Owned<IHandler>> factory)
//   {
//       Process: for each message -> using (var h = factory()) h.Value.Handle();
//   }
//
// Each iteration begins and ends a short-lived Owned<T> scope on the fly. When
// the using block ends, that unit of work's disposables are released, and every
// unit is independent of the others.
//
// Reference implementation: Autofac.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.MessagePump;

public class MessagePumpTests
{
    [Fact]
    public void Every_unit_of_work_is_disposed_when_its_owned_scope_ends()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            IMessagePump pump = container.Resolve<IMessagePump>();

            pump.Process(3);

            pump.Handled.Count.ShouldBe(3);
            pump.Handled.ShouldAllBe(handler => handler.Connection.IsDisposed);
            pump.Handled.Select(handler => handler.Connection).Distinct().Count().ShouldBe(3);
        }
#endif
        {
            Composition composition = new Composition();
            IMessagePump pump = composition.Pump;

            pump.Process(3);

            pump.Handled.Count.ShouldBe(3);
            pump.Handled.ShouldAllBe(handler => handler.Connection.IsDisposed);
            pump.Handled.Select(handler => handler.Connection).Distinct().Count().ShouldBe(3);
        }
    }

#if AUTOFAC_REFERENCE
    private static IContainer BuildAutofac()
    {
        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterType<Connection>().As<IConnection>().InstancePerDependency();
        builder.RegisterType<Handler>().As<IHandler>().InstancePerDependency();
        builder.RegisterType<AutofacMessagePump>().As<IMessagePump>().InstancePerDependency();
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

interface IHandler
{
    IConnection Connection { get; }

    void Handle();
}

sealed class Handler(IConnection connection) : IHandler
{
    public IConnection Connection { get; } = connection;

    public void Handle()
    {
    }
}

interface IMessagePump
{
    IReadOnlyList<IHandler> Handled { get; }

    void Process(int count);
}

sealed class MessagePump(Func<Owned<IHandler>> handlerFactory) : IMessagePump
{
    private readonly List<IHandler> handled = [];

    public IReadOnlyList<IHandler> Handled => handled;

    public void Process(int count)
    {
        for (int i = 0; i < count; i++)
        {
            using Owned<IHandler> handler = handlerFactory();
            handler.Value.Handle();
            handled.Add(handler.Value);
        }
    }
}

#if AUTOFAC_REFERENCE
sealed class AutofacMessagePump(
    Func<Autofac.Features.OwnedInstances.Owned<IHandler>> handlerFactory) : IMessagePump
{
    private readonly List<IHandler> handled = [];

    public IReadOnlyList<IHandler> Handled => handled;

    public void Process(int count)
    {
        for (int i = 0; i < count; i++)
        {
            using Autofac.Features.OwnedInstances.Owned<IHandler> handler = handlerFactory();
            handler.Value.Handle();
            handled.Add(handler.Value);
        }
    }
}
#endif

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind().To<Connection>()
            .Bind().To<Handler>()
            .Bind().To<MessagePump>()
            .Root<IMessagePump>("Pump");
}
