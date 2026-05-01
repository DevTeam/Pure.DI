/*
$v=true
$p=5
$d=Root arguments
$h=Use root arguments when you need to pass state into a specific root. Define them with `RootArg<T>(string argName)` (optionally with tags) and use them like any other dependency. A root that uses at least one root argument becomes a method, and only arguments used in that root's object graph appear in the method signature. Use unique argument names to avoid collisions.
$h=Root arguments are useful when runtime values belong to one entry point, not to the whole composition.
$h=>[!NOTE]
$h=>Actually, root arguments work like normal bindings. The difference is that they bind to the values of the arguments. These values will be injected wherever they are required.
$h=
$f=When using root arguments, compilation warnings are emitted if `Resolve` methods are generated because these methods cannot create such roots. Disable `Resolve` via `Hint(Hint.Resolve, "Off")`, or ignore the warnings and accept the risks.
$r=Shouldly
$f=Limitations: roots with root arguments become methods and are incompatible with generated `Resolve` methods.
$f=Common pitfalls:
$f=- Reusing ambiguous argument names for different concepts.
$f=- Forgetting to disable or avoid `Resolve` usage in these setups.
$f=See also: [Composition arguments](composition-arguments.md), [Resolve hint](resolve-hint.md).
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.RootArgumentsScenarioNullable;

using Shouldly;
using Xunit;
using static Tag;

// {
//# using Pure.DI;
//# using static Pure.DI.Tag;
// }

public class ScenarioNullable
{
    [Fact]
    public void Run()
    {
        // Root arguments make Resolve unusable, so disable Resolve generation
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // Disable Resolve methods because root arguments are not compatible
            .Hint(Hint.Resolve, "Off")
            .Bind<IDatabaseServiceNullable>().To<DatabaseServiceNullable>()
            .Bind<IApplicationNullable>().To<ApplicationNullable>()

            // Root arguments serve as values passed
            // to the composition root method
            .RootArg<int?>("port")
            .RootArg<string?>("connectionString")

            // An argument can be tagged
            // to be injectable by type and this tag
            .RootArg<string>("appName", AppDetail)

            // Composition root
            .Root<IApplicationNullable>("CreateApplication");

        var composition = new Composition();
        
        string? connectionString = null;
        int? port = 8080;

        // Creates an application with specific arguments
        var app = composition.CreateApplication(
            appName: "MySuperApp",
            port: port,
            connectionString: connectionString);

        app.Name.ShouldBe("MySuperApp");
        app.Database.Port.ShouldBe(8080);
        app.Database.ConnectionString.ShouldBeNull();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDatabaseServiceNullable
{
    int? Port { get; }

    string? ConnectionString { get; }
}

class DatabaseServiceNullable(int? port, string? connectionString) : IDatabaseServiceNullable
{
    public int? Port { get; } = port;

    public string? ConnectionString { get; } = connectionString;
}

interface IApplicationNullable
{
    string Name { get; }

    IDatabaseServiceNullable Database { get; }
}

class ApplicationNullable(
    [Tag(AppDetail)] string name,
    IDatabaseServiceNullable database)
    : IApplicationNullable
{
    public string Name { get; } = name;

    public IDatabaseServiceNullable Database { get; } = database;
}
// }
