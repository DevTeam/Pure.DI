/*
$v=true
$p=15
$d=Required properties or fields
$h=This example demonstrates how the `required` modifier can be used to automatically inject dependencies into properties and fields. When a property or field is marked with `required`, the DI will automatically inject the dependency without additional effort.
$f=This approach simplifies dependency injection by eliminating the need to manually configure bindings for required dependencies, making the code more concise and easier to maintain.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.RequiredPropertiesOrFieldsScenario;

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
            .Arg<string>("connectionString")
            .Bind<IDatabase>().To<SqlDatabase>()
            .Bind<IUserRepository>().To<UserRepository>()

            // Composition root
            .Root<IUserRepository>("Repository");

        var composition = new Composition(connectionString: "Server=.;Database=MyDb;");
        var repository = composition.Repository;

        repository.Database.ShouldBeOfType<SqlDatabase>();
        repository.ConnectionString.ShouldBe("Server=.;Database=MyDb;");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDatabase;

class SqlDatabase : IDatabase;

interface IUserRepository
{
    string ConnectionString { get; }

    IDatabase Database { get; }
}

class UserRepository : IUserRepository
{
    // The required field will be injected automatically.
    // In this case, it gets the value from the composition argument
    // of type 'string'.
    public required string ConnectionStringField;

    public string ConnectionString => ConnectionStringField;

    // The required property will be injected automatically
    // without additional effort.
    public required IDatabase Database { get; init; }
}
// }