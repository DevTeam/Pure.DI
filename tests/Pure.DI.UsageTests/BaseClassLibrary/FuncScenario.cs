/*
$v=true
$p=0
$d=Func
$h=_Func<T>_ helps when the logic must enter instances of some type on demand or more than once. This is a very handy mechanism for instance replication. For example it is used when implementing the `Lazy<T>` injection.
$f=Be careful, replication takes into account the lifetime of the object.
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
        // This hint indicates to not generate methods such as Resolve
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