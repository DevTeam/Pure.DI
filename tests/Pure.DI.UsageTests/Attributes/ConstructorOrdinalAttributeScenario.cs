/*
$v=true
$p=0
$d=Constructor ordinal attribute
$sa=Member ordinal attribute
$sa=Dependency attribute
$h=Applying this attribute disables automatic constructor selection. Only constructors marked with this attribute are considered, ordered by ordinal (ascending).
$f=The `Ordinal` attribute is part of the API, but you can define your own in any assembly or namespace.
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

[SuppressMessage("WRN", "DIW003:WRN")]
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
    // The DI will try to use this constructor first (Ordinal 0).
    [Ordinal(0)]
    internal SqlDatabaseClient(string connectionString) =>
        ConnectionString = connectionString;

    // If the first constructor cannot be used (e.g. connectionString is missing),
    // the DI will try to use this one (Ordinal 1).
    [Ordinal(1)]
    public SqlDatabaseClient(IConfiguration configuration) =>
        ConnectionString = "Server=.;Database=DefaultDb;";

    public SqlDatabaseClient() =>
        ConnectionString = "InMemory";

    public string ConnectionString { get; }
}
// }
