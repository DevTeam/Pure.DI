/*
$v=true
$t=BaseClassLibrary
$t=HighPerformance
$p=0
$p=HighPerformance:10
$d=Func
$sa=Func with arguments
$sa=Func with tag
$sa=Injection on demand
$sa=Lazy
$h=_Func<T>_ helps when the logic must enter instances of some type on demand or more than once. This is a very handy mechanism for instance replication. For example it is used when implementing the `Lazy<T>` injection.
$f=Be careful, replication takes into account the lifetime of the object. Here `TicketIdGenerator` is a `Singleton`, so the three tickets share one counter and receive ids `1`, `2` and `3`, while each `Ticket` itself is transient.
$f=This is the standard way to hand a consumer the ability to create instances later — a queue terminal, a job factory, a pooled worker — without giving it the container. The delegate is generated code, so the call does not go through reflection.
$f=A `Func<T>` around a `Singleton` returns the same instance on every call, which surprises code that expects fresh objects. Match the lifetime of the requested type to the intent: transient for replication, singleton when the factory is only needed to defer the first use.
$f=>[!TIP]
$f=>Use `Func<T>` for repeated creation and [`Lazy<T>`](lazy.md) when one deferred instance is enough.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.BCL.FuncScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Immutable;
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
            .Bind().As(Lifetime.Singleton).To<TicketIdGenerator>()
            .Bind().To<Ticket>()
            .Bind().To<QueueTerminal>()

            // Composition root
            .Root<IQueueTerminal>("Terminal");

        var composition = new Composition();
        var terminal = composition.Terminal;

        terminal.Tickets.Length.ShouldBe(3);

        terminal.Tickets[0].Id.ShouldBe(1);
        terminal.Tickets[1].Id.ShouldBe(2);
        terminal.Tickets[2].Id.ShouldBe(3);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface ITicketIdGenerator
{
    int NextId { get; }
}

class TicketIdGenerator : ITicketIdGenerator
{
    public int NextId => ++field;
}

interface ITicket
{
    int Id { get; }
}

class Ticket(ITicketIdGenerator idGenerator) : ITicket
{
    public int Id { get; } = idGenerator.NextId;
}

interface IQueueTerminal
{
    ImmutableArray<ITicket> Tickets { get; }
}

class QueueTerminal(Func<ITicket> ticketFactory) : IQueueTerminal
{
    public ImmutableArray<ITicket> Tickets { get; } =
    [
        // The factory creates a new instance of the ticket each time it is called
        ticketFactory(),
        ticketFactory(),
        ticketFactory()
    ];
}
// }
