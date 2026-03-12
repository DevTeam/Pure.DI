/*
$v=true
$p=8
$d=Simplified lifetime-specific factory
$h=Demonstrates how to create factories with lifetime-specific bindings, providing a concise way to define factories with proper lifetime semantics.
$f=>[!NOTE]
$f=>Lifetime-specific factories combine the convenience of simplified syntax with explicit lifetime control for optimal performance and correctness.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedVariable
namespace Pure.DI.UsageTests.Basics.SimplifiedLifetimeFactoryScenario;

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
            .Transient(Guid.NewGuid)
            .Transient(() => DateTime.Today, "today")
            // Injects FileLogger and DateTime instances
            // and performs further initialization logic
            // defined in the lambda function to set up the log file name
            .Singleton<FileLogger, DateTime, IFileLogger>((
                logger,
                [Tag("today")] date) => {
                logger.Init($"app-{date:yyyy-MM-dd}.log");
                return logger;
            })
            .Transient<OrderProcessingService>()

            // Composition root
            .Root<IOrderProcessingService>("OrderService");

        var composition = new Composition();
        var service = composition.OrderService;

        service.Logger.FileName.ShouldBe($"app-{DateTime.Today:yyyy-MM-dd}.log");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IFileLogger
{
    string FileName { get; }

    void Log(string message);
}

class FileLogger(Func<Guid> idFactory) : IFileLogger
{
    public string FileName { get; private set; } = "";

    public void Init(string fileName) => FileName = fileName;

    public void Log(string message)
    {
        var id = idFactory();
        // Write to file
    }
}

interface IOrderProcessingService
{
    IFileLogger Logger { get; }
}

class OrderProcessingService(IFileLogger logger) : IOrderProcessingService
{
    public IFileLogger Logger { get; } = logger;
}
// }