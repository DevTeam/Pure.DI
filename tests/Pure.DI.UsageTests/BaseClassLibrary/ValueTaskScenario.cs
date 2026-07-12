/*
$v=true
$t=BaseClassLibrary
$t=HighPerformance
$p=10
$p=HighPerformance:3
$d=ValueTask
$sa=Task
$h=A dependency can be injected as `ValueTask<T>` and awaited when needed — an allocation-friendly alternative to `Task<T>` for values that are usually available synchronously. Bind the underlying type as usual and request `ValueTask<T>`; here `DataProcessor` awaits `ValueTask<IConnection>` before using the connection.
$f=>[!NOTE]
$f=>`ValueTask<T>` reduces allocations compared to `Task<T>` when operations complete synchronously, making it ideal for high-performance scenarios.
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
