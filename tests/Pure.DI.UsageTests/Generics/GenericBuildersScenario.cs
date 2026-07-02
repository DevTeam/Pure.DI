/*
$v=true
$p=9
$d=Generic builders
$h=Sometimes many related types need the same build-up treatment. A single `Builders<IMessage<TT, TT2>>("BuildUp")` call creates a builder for every type implementing `IMessage<TT, TT2>` that is visible at compile time — here both `QueryMessage<,>` and `CommandMessage<,>` get their own `BuildUp` overload.
$h=Each builder injects members marked with the `[Dependency]` attribute into an existing instance, so instances created elsewhere (e.g. by an API controller) still receive their dependencies.
$f=>[!NOTE]
$f=>Generic builders provide compile-time type safety while allowing flexible object graph construction.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable RedundantArgumentDefaultValue
namespace Pure.DI.UsageTests.Generics.GenericBuildersScenario;

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
            .Bind(Tag.Id).To(() => (TT)(object)Guid.NewGuid())
            .Bind().To<MessageTracker<TT>>()
            // Generic builder to inject dependencies into existing messages
            .Builders<IMessage<TT, TT2>>("BuildUp");

        var composition = new Composition();

        // A Query is created (e.g. by API controller), ID is missing
        var query = new QueryMessage<Guid, string>();

        // Composition injects dependencies and generates an ID
        var queryWithDeps = composition.BuildUp(query);

        queryWithDeps.Id.ShouldNotBe(Guid.Empty);
        queryWithDeps.Tracker.ShouldBeOfType<MessageTracker<string>>();

        // A Command is created, usually with a specific ID
        var command = new CommandMessage<Guid, int>();

        // Composition injects dependencies only
        var commandWithDeps = composition.BuildUp(command);

        commandWithDeps.Id.ShouldBe(Guid.Empty);
        commandWithDeps.Tracker.ShouldBeOfType<MessageTracker<int>>();

        // Works with abstract types/interfaces too
        var queryMessage = new QueryMessage<Guid, double>();
        queryMessage = composition.BuildUp(queryMessage);

        queryMessage.ShouldBeOfType<QueryMessage<Guid, double>>();
        queryMessage.Id.ShouldNotBe(Guid.Empty);
        queryMessage.Tracker.ShouldBeOfType<MessageTracker<double>>();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IMessageTracker<T>;

class MessageTracker<T> : IMessageTracker<T>;

interface IMessage<out TId, TContent>
{
    TId Id { get; }

    IMessageTracker<TContent>? Tracker { get; }
}

record QueryMessage<TId, TContent> : IMessage<TId, TContent>
    where TId : struct
{
    public TId Id { get; private set; }

    [Dependency]
    public IMessageTracker<TContent>? Tracker { get; set; }

    // Injects a new ID
    [Dependency]
    public void SetId([Tag(Tag.Id)] TId id) => Id = id;
}

record CommandMessage<TId, TContent> : IMessage<TId, TContent>
    where TId : struct
{
    public TId Id { get; }

    [Dependency]
    public IMessageTracker<TContent>? Tracker { get; set; }
}
// }