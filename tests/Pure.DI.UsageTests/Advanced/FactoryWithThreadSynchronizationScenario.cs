/*
$v=true
$p=2
$h=In some cases, initialization of objects requires synchronization of the overall composition flow.
$d=Factory with thread synchronization
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
        // This hint indicates to not generate methods such as Resolve
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