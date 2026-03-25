/*
$v=true
$p=1
$d=ThreadSafe hint
$h=Hints are used to fine-tune code generation. The `ThreadSafe` hint determines whether object composition will be created in a thread-safe manner. This hint is `On` by default. It is good practice not to use threads when creating an object graph, in which case this hint can be turned off, which will lead to a slight increase in performance.
$h=In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// ThreadSafe = Off`.
$f=For more hints, see [this](../README.md#setup-hints) page.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Hints.ThreadSafeHintScenario;

using Xunit;
using static Hint;

// {
//# using Pure.DI;
//# using static Pure.DI.Hint;
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
            // Disabling thread-safety can improve performance.
            // This is safe when the object graph is resolved on a single thread,
            // for example at application startup.
            .Hint(ThreadSafe, "Off")
            .Bind().To<SqlDatabaseConnection>()
            .Bind().As(Lifetime.Singleton).To<ReportGenerator>()
            .Root<IReportGenerator>("Generator");

        var composition = new Composition();
        var reportGenerator = composition.Generator;
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDatabaseConnection;

class SqlDatabaseConnection : IDatabaseConnection;

interface IReportGenerator;

class ReportGenerator(Func<IDatabaseConnection> connectionFactory) : IReportGenerator;
// }