/*
$v=true
$p=5
$d=Generic async composition roots with constraints
$h=> [!IMPORTANT]
$h=> `Resolve' methods cannot be used to resolve generic composition roots.
$f=> [!IMPORTANT]
$f=> The method `Inject()`cannot be used outside of the binding setup.
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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // This hint indicates to not generate methods such as Resolve
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