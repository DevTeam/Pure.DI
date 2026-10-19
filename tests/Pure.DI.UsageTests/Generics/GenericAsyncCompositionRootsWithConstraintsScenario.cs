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
$f=
$f=Asynchronous generic roots are what a data access layer or an HTTP client layer exposes: the caller picks the connection and the result type, and receives a ready-to-use query. The `CancellationToken` root argument keeps cancellation in the caller's hands instead of hiding it in the composition.
$f=A `Task<T>` root builds the graph when the method is awaited, so the composition stays in control of construction order while the caller controls timing. Every await starts a new resolution, which is why transient and `PerResolve` dependencies are not shared between two calls.
$f=>[!WARNING]
$f=>The generic parameters of the root come from the marker types of the root declaration, so their constraints are the contract. Loosen a constraint on the root and the implementation may no longer be constructible; the error appears at the binding, not at the call site.
$f=
$f=Prefer a synchronous generic root when no asynchronous work happens during construction; reach for `Task<T>` only when a dependency is genuinely asynchronous. See [Generic composition roots with constraints](generic-composition-roots-with-constraints.md) for the synchronous form.
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