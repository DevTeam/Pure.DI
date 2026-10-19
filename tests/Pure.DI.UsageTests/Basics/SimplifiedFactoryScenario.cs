/*
$v=true
$t=QuickStart
$t=Basics
$p=6
$p=QuickStart:9
$d=Simplified factory
$sa=Factory
$sa=Simplified lifetime-specific factory
$h=This example shows a simplified manual factory. Each lambda parameter represents an injected dependency, and starting with C# 10 you can add `Tag(...)` to specify a tagged dependency.
$h=The lambda replaces the whole `ctx.Inject` boilerplate of a factory binding: dependencies arrive as parameters, so the body only holds the initialization logic.
$f=The example creates a service that depends on a logger initialized with a date-based file name.
$f=This style keeps the setup concise while still allowing explicit initialization logic.
$f=The `Tag` attribute enables named dependencies for more complex setups; without it, the parameter takes the untagged binding, and an ambiguous pair of same-type parameters fails the compilation.
$r=Shouldly
$f=Limitations: compact lambda factories stay readable only while initialization logic remains small.
$f=The lambda runs each time the binding is resolved, so the same initialization happens per instance under a transient lifetime; make the binding a singleton when the setup must happen once.
$f=Prefer it to `To(ctx => ...)` when the factory needs dependencies but no overrides, and prefer a plain constructor when there is nothing to initialize.
$f=Common pitfalls:
$f=- Putting heavy imperative setup code into short lambda factories.
$f=- Forgetting explicit tags when several same-type dependencies exist.
$f=See also: [Factory](factory.md), [Tags](tags.md).
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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind("today").To(() => DateTime.Today)
            // Injects FileLogger and DateTime
            // and applies additional initialization logic
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
