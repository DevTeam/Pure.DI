/*
$v=true
$t=BaseClassLibrary
$t=HighPerformance
$p=10
$p=HighPerformance:8
$d=ValueTask
$sa=Task
$h=A dependency can be injected as `ValueTask<T>` and awaited when needed — an allocation-friendly alternative to `Task<T>` for values that are usually available synchronously. Bind the underlying type as usual and request `ValueTask<T>`; here `DataProcessor` awaits `ValueTask<IConnection>` before using the connection.
$f=>[!NOTE]
$f=>`ValueTask<T>` reduces allocations compared to `Task<T>` when operations complete synchronously, making it ideal for high-performance scenarios.
$f=
$f=The consumer awaits the value before using it, so nothing about the connection is created eagerly on the calling thread; `ProcessDataAsync` is already asynchronous when it joins the value. Use it when the dependency is usually ready synchronously but must remain awaitable.
$f=A `ValueTask` may only be awaited once, so do not store it in a field and consume it repeatedly — request `Task<T>` instead when the consumer needs to await from several places. This is the main trap when replacing `Task<T>` with `ValueTask<T>` in an existing constructor.
$f=>[!TIP]
$f=>Prefer [`Task<T>`](task.md) for shared or multi-await scenarios and `ValueTask<T>` when the value is awaited once on a hot path.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.BCL.ValueTaskScenario;

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
            .Bind<IConnection>().To<CloudConnection>()
            .Bind<IDataProcessor>().To<DataProcessor>()

            // Composition root
            .Root<IDataProcessor>("DataProcessor");

        var composition = new Composition();
        var processor = composition.DataProcessor;
        await processor.ProcessDataAsync();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IConnection
{
    ValueTask<bool> PingAsync();
}

class CloudConnection : IConnection
{
    public ValueTask<bool> PingAsync() => ValueTask.FromResult(true);
}

interface IDataProcessor
{
    ValueTask ProcessDataAsync();
}

class DataProcessor(ValueTask<IConnection> connectionTask) : IDataProcessor
{
    public async ValueTask ProcessDataAsync()
    {
        // The connection is resolved asynchronously, allowing potential
        // non-blocking initialization or resource allocation.
        var connection = await connectionTask;
        await connection.PingAsync();
    }
}
// }
