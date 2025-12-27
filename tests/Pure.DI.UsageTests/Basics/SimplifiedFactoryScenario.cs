/*
$v=true
$p=2
$d=Simplified factory
$h=This example shows how to create and initialize an instance manually in a simplified form. When you use a lambda function to specify custom instance initialization logic, each parameter of that function represents an injection of a dependency. Starting with C# 10, you can also put the `Tag(...)` attribute in front of the parameter to specify the tag of the injected dependency.
$f=The example creates a `service` that depends on a `logger` initialized with a specific file name based on the current date. The `Tag` attribute allows specifying named dependencies for more complex scenarios.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedParameter.Global
namespace Pure.DI.UsageTests.Basics.SimplifiedFactoryScenario;

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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind("today").To(() => DateTime.Today)
            // Injects FileLogger and DateTime instances
            // and performs further initialization logic
            // defined in the lambda function to set up the log file name
            .Bind<IFileLogger>().To((
                FileLogger logger,
                [Tag("today")] DateTime date) => {
                logger.Init($"app-{date:yyyy-MM-dd}.log");
                return logger;
            })
            .Bind().To<OrderProcessingService>()

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

class FileLogger : IFileLogger
{
    public string FileName { get; private set; } = "";

    public void Init(string fileName) => FileName = fileName;

    public void Log(string message)
    {
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