/*
$v=true
$p=1
$d=ThreadSafe hint
$h=Hints are used to fine-tune code generation. The `ThreadSafe` hint determines whether object composition will be created in a thread-safe manner. This hint is `On` by default. It is good practice not to use threads when creating an object graph, in which case this hint can be turned off, which will lead to a slight increase in performance.
$h=Turning it off is a statement about how the composition is used: it is right for a graph built once during single-threaded startup, for build and CLI tools, and for tests, and wrong for any composition shared by threads while singletons are still being created.
$f=In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// ThreadSafe = Off`.
$f=With the hint on, the generated code synchronizes the creation of stored instances, so two threads asking for a singleton for the first time get one instance instead of two.
$f=With the hint off, the lock disappears from generated code and nothing warns you later: a singleton created under two threads can be built twice, and fields are written without synchronization.
$f=A factory that mutates shared state from `ctx.Override` needs its own synchronization when this hint is off - Pure.DI runs the code unsynchronized exactly as written.
$f=Prefer the default `On` for anything a UI or a server resolves at runtime, and switch it off only where the composition is created and fully resolved on one thread before the threads start.
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