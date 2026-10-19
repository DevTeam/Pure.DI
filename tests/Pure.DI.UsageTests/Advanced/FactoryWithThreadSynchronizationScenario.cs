/*
$v=true
$p=20
$d=Factory with thread synchronization
$sa=Factory
$sa=Thread-safe overrides
$h=In some cases, initialization of objects requires synchronization of the overall composition flow. This scenario demonstrates how to use factories with thread synchronization to ensure proper initialization order.
$h=`ctx.Lock` is the object the generated code already uses for its own synchronization, so a custom factory locks the same object instead of introducing a second lock that could deadlock against it.
$f=This is for a service that must connect once during construction - a broker connection, a cache warm-up, a first handshake - while other threads may resolve the same graph at the same time.
$f=The lock covers the injection and the initialization together, so no other resolve of the same composition can observe the object half-initialized.
$f=>[!NOTE]
$f=>Thread synchronization in factories should be used carefully as it may impact performance. Only use when necessary for correct initialization behavior.
$f=
$f=Prefer `ctx.IsLockRequired` ([IsLockRequired](islockrequired.md)) when the lock should be taken only in thread-safe compositions, and `Hint.ThreadSafe = Off` ([ThreadSafe hint](threadsafe-hint.md)) for a composition that is known to be single-threaded.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.Advanced.FactoryWithThreadSynchronizationScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind<IMessageBus>().To<IMessageBus>(ctx => {
                // Initialization logic requiring synchronization
                // of the overall composition flow.
                // For example, connecting to a message broker.
                lock (ctx.Lock)
                {
                    ctx.Inject(out MessageBus bus);
                    bus.Connect();
                    return bus;
                }
            })
            .Bind<INotificationService>().To<NotificationService>()

            // Composition root
            .Root<INotificationService>("NotificationService");

        var composition = new Composition();
        var service = composition.NotificationService;
        service.Bus.IsConnected.ShouldBeTrue();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IMessageBus
{
    bool IsConnected { get; }
}

class MessageBus : IMessageBus
{
    public bool IsConnected { get; private set; }

    public void Connect() => IsConnected = true;
}

interface INotificationService
{
    IMessageBus Bus { get; }
}

class NotificationService(IMessageBus bus) : INotificationService
{
    public IMessageBus Bus { get; } = bus;
}
// }