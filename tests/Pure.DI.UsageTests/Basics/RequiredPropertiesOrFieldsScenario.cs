/*
$v=true
$p=19
$d=Required properties or fields
$sa=Property injection
$sa=Default values
$h=This example shows how the `required` modifier can be used to automatically inject dependencies into properties and fields. When a property or field is marked with `required`, the DI will automatically inject the dependency without additional effort.
$h=It suits immutable entities and records that must be fully initialized before use: the language already guarantees that every `required` member is set, and Pure.DI supplies the values.
$f=No `[Dependency]` attribute is needed, but a `required` member without a matching binding is a graph error, not a silent `null`.
$f=`required` members are injected after the attributed ones: they take the highest ordinal unless you give them an explicit `[Ordinal]`.
$f=An `init` setter works because the generated code assigns the member during initialization; a `required` field follows the same rule and must not be `readonly`.
$f=Prefer `required` for mandatory dependencies of immutable types, `[Dependency]` for optional ones, and constructor injection when the type is not a record or DTO.
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
        // Disable Resolve methods to keep the public API minimal
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