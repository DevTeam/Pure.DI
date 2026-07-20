/*
$v=true
$p=107
$d=Tracing exceptions during Owned disposal
$sa=Tracing exceptions during composition disposal
$sa=Tracking disposable instances per a composition root
$h=`Owned<T>` catches exceptions thrown while disposing resources in its graph, invokes the `OnDisposeException` partial method on the generated `Pure.DI.Owned` accumulator, and continues disposing the remaining resources. Implement this hook to associate per-operation cleanup failures with application diagnostics.
$f=>[!IMPORTANT]
$f=>The hook belongs to the non-generic `Pure.DI.Owned` accumulator used internally by every `Owned<T>`, so its partial implementation must be declared in the `Pure.DI` namespace. The partial `Owned<T>` extension exposes only the events collected by its own accumulator.
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers

namespace Pure.DI.UsageTests.Advanced.TracingOwnedDisposalExceptionsScenario
{
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
// {
            var composition = new Composition();
            var messageHandler = composition.MessageHandler;

            // Ends processing of one message. The broker consumer fails to
            // close, but the checkpoint writer must still be released.
            messageHandler.Dispose();

            messageHandler.Events.ShouldBe([
                "BrokerConsumer: The broker did not acknowledge consumer shutdown."]);
            messageHandler.Value.CheckpointWriter.IsDisposed.ShouldBeTrue();
// }
            composition.SaveClassDiagram();
        }
    }

// {
    interface ICheckpointWriter
    {
        bool IsDisposed { get; }
    }

    // Represents a per-message checkpoint buffer that must always be released.
    class CheckpointWriter : ICheckpointWriter, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    interface IBrokerConsumer;

    // Represents a per-message broker consumer that can fail while closing.
    class BrokerConsumer : IBrokerConsumer, IDisposable
    {
        public void Dispose() =>
            throw new IOException("The broker did not acknowledge consumer shutdown.");
    }

    interface IMessageHandler
    {
        ICheckpointWriter CheckpointWriter { get; }
    }

    class MessageHandler(
        ICheckpointWriter checkpointWriter,
        IBrokerConsumer brokerConsumer)
        : IMessageHandler
    {
        public ICheckpointWriter CheckpointWriter { get; } = checkpointWriter;

        public IBrokerConsumer BrokerConsumer { get; } = brokerConsumer;
    }

    partial class Composition
    {
        static void Setup() =>
// }
            // Disable Resolve methods to keep the public API minimal
            // Resolve = Off
// {
            DI.Setup()
                .Bind<ICheckpointWriter>().To<CheckpointWriter>()
                .Bind<IBrokerConsumer>().To<BrokerConsumer>()
                .Bind<IMessageHandler>().To<MessageHandler>()
                .Root<Owned<IMessageHandler>>("MessageHandler");
    }
// }
}

// The hook is compiled from a separate partial-class file because this scenario
// also declares its application types in another namespace.
// {
//# namespace Pure.DI
//# {
//#     internal sealed partial class Owned
//#     {
//#         private readonly List<string> _events = [];
//#
//#         // Called whenever a resource in any Owned<T> graph fails to dispose.
//#         partial void OnDisposeException<T>(T disposableInstance, Exception exception)
//#             where T : IDisposable =>
//#             _events.Add($"{disposableInstance.GetType().Name}: {exception.Message}");
//#
//#         public IReadOnlyList<string> Events => _events;
//#     }
//#
//#     internal readonly partial struct Owned<T>
//#     {
//#         // Exposes diagnostics collected for this Owned<T> graph only.
//#         public IReadOnlyList<string> Events => ((Owned)owned).Events;
//#     }
//# }
// }
