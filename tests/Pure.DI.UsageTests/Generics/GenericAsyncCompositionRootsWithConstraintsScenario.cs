/*
$v=true
$p=3
$d=Generic async composition roots with constraints
$sa=Generic composition roots with constraints
$h=Generic composition roots can be asynchronous and constrained at the same time. Constrained marker types — `TTDisposable` (`IDisposable`) and `TTS` (`struct`) — carry their constraints into the generated methods, and wrapping the root type in `Task<...>` yields methods like `GetDataQueryAsync<T, TStruct>(CancellationToken)` that build the object graph asynchronously.
$h=The `CancellationToken` comes from a `RootArg` and is passed in at resolution time.
$h=>[!IMPORTANT]
$h=>`Resolve` methods cannot be used to resolve generic composition roots.
$f=>[!IMPORTANT]
$f=>The method `Inject()` cannot be used outside of the binding setup.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedTypeParameter

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Generics.GenericAsyncCompositionRootsWithConstraintsScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // Disable Resolve methods to keep the public API minimal
            .Hint(Hint.Resolve, "Off")
            .Bind().To<ConnectionProvider<TTDisposable>>()
            .Bind().To<DataQuery<TTDisposable, TTS>>()
            // Creates StatusQuery manually,
            // just for the sake of example
            .Bind("Status").To(ctx => {
                ctx.Inject(out IConnectionProvider<TTDisposable> connectionProvider);
                return new StatusQuery<TTDisposable>(connectionProvider);
            })

            // Specifies to use CancellationToken from the argument
            // when resolving a composition root
            .RootArg<CancellationToken>("cancellationToken")

            // Specifies to create a regular public method
            // to get a composition root of type Task<DataQuery<T, TStruct>>
            // with the name "GetDataQueryAsync"
            .Root<Task<IQuery<TTDisposable, TTS>>>("GetDataQueryAsync")

            // Specifies to create a regular public method
            // to get a composition root of type Task<StatusQuery<T>>
            // with the name "GetStatusQueryAsync"
            // using the "Status" tag
            .Root<Task<IQuery<TTDisposable, bool>>>("GetStatusQueryAsync", "Status");

        var composition = new Composition();

        // Resolves composition roots asynchronously
        var query = await composition.GetDataQueryAsync<Stream, double>(CancellationToken.None);
        var status = await composition.GetStatusQueryAsync<BinaryReader>(CancellationToken.None);
// }
        query.ShouldBeOfType<DataQuery<Stream, double>>();
        status.ShouldBeOfType<StatusQuery<BinaryReader>>();
        composition.SaveClassDiagram();
    }
}

// {
interface IConnectionProvider<T>
    where T : IDisposable;

class ConnectionProvider<T> : IConnectionProvider<T>
    where T : IDisposable;

interface IQuery<TConnection, TResult>
    where TConnection : IDisposable
    where TResult : struct;

class DataQuery<TConnection, TResult>(IConnectionProvider<TConnection> connectionProvider)
    : IQuery<TConnection, TResult>
    where TConnection : IDisposable
    where TResult : struct;

class StatusQuery<TConnection>(IConnectionProvider<TConnection> connectionProvider)
    : IQuery<TConnection, bool>
    where TConnection : IDisposable;
// }