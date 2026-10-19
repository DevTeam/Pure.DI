/*
$v=true
$p=12
$d=Generic builders
$sa=Generic builder
$sa=Builders
$h=Sometimes many related types need the same build-up treatment. A single `Builders<IMessage<TT, TT2>>("BuildUp")` call creates a builder for every type implementing `IMessage<TT, TT2>` that is visible at compile time — here both `QueryMessage<,>` and `CommandMessage<,>` get their own `BuildUp` overload.
$h=Each builder injects members marked with the `[Dependency]` attribute into an existing instance, so instances created elsewhere (e.g. by an API controller) still receive their dependencies.
$f=>[!NOTE]
$f=>Generic builders provide compile-time type safety while allowing flexible object graph construction.
$f=
$f=This is the pragmatic middle ground for a message pipeline: many message shapes, each created by deserialization or by an API layer, and all of them needing the same tracker or context injected. One declaration replaces a builder per message type.
$f=The builders are generated for the implementations visible at compile time at the call site, and each overload is chosen by the static type of the argument. Passing a variable declared as `IMessage<TId, TContent>` resolves the interface overload, so the concrete type may not receive its own injections.
$f=>[!WARNING]
$f=>Every implementation gets an overload, so the generated API grows with the number of implementing types. Keep the call on an assembly where that set is stable, otherwise public surface changes with unrelated code.
$f=
$f=Prefer a single [Generic builder](generic-builder.md) when only one type needs building, and explicit `BuildUp` roots when the set of types is small and you want the API to stay fixed.
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