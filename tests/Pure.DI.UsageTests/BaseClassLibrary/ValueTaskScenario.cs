/*
$v=true
$p=3
$d=ValueTask
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
        // This hint indicates to not generate methods such as Resolve
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