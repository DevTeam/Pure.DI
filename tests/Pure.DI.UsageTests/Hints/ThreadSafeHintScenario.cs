/*
$v=true
$p=1
$d=ThreadSafe hint
$h=Hints are used to fine-tune code generation. The _ThreadSafe_ hint determines whether object composition will be created in a thread-safe manner. This hint is _On_ by default. It is good practice not to use threads when creating an object graph, in which case this hint can be turned off, which will lead to a slight increase in performance.
$h=In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// ThreadSafe = Off`.
$f=For more hints, see [this](README.md#setup-hints) page.
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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // Отключение потокобезопасности в композиции может повысить производительность.
            // Это безопасно, если граф объектов разрешается в одном потоке,
            // например, при запуске приложения.
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