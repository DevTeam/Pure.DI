/*
$v=true
$p=0
$d=Constructor ordinal attribute
$h=When applied to any constructor in a type, automatic injection constructor selection is disabled. The selection will only focus on constructors marked with this attribute, in the appropriate order from smallest value to largest.
$f=The attribute `Ordinal` is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable RedundantArgumentDefaultValue

namespace Pure.DI.UsageTests.Attributes.ConstructorOrdinalAttributeScenario;

using System.Diagnostics.CodeAnalysis;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

[SuppressMessage("WRN", "DIW001:WRN")]
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
            .Bind().To<Configuration>()
            .Bind().To<SqlDatabaseClient>()

            // Composition root
            .Root<IDatabaseClient>("Client");

        var composition = new Composition(connectionString: "Server=.;Database=MyDb;");
        var client = composition.Client;

        // The client was created using the connection string constructor (Ordinal 0)
        // even though the configuration constructor (Ordinal 1) was also possible.
        client.ConnectionString.ShouldBe("Server=.;Database=MyDb;");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IConfiguration;

class Configuration : IConfiguration;

interface IDatabaseClient
{
    string ConnectionString { get; }
}

class SqlDatabaseClient : IDatabaseClient
{
    // The integer value in the argument specifies
    // the ordinal of injection.
    // The DI container will try to use this constructor first (Ordinal 0).
    [Ordinal(0)]
    internal SqlDatabaseClient(string connectionString) =>
        ConnectionString = connectionString;

    // If the first constructor cannot be used (e.g. connectionString is missing),
    // the DI container will try to use this one (Ordinal 1).
    [Ordinal(1)]
    public SqlDatabaseClient(IConfiguration configuration) =>
        ConnectionString = "Server=.;Database=DefaultDb;";

    public SqlDatabaseClient() =>
        ConnectionString = "InMemory";

    public string ConnectionString { get; }
}
// }