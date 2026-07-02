/*
$v=true
$p=99
$d=Func with tag
$h=A tag applied to a `Func<T>` dependency carries over to the instances it creates. Here `[Tag("postgres")] Func<IDbConnection>` resolves the binding registered with the `"postgres"` tag, and each call returns a new `NpgsqlConnection`, letting the pool create as many distinct connections as it needs.
$f=>[!NOTE]
$f=>Func with tags allows you to create instances with specific tags dynamically, useful for factory patterns with multiple implementations.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.BCL.FuncWithTagScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Immutable;
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
            .Bind<IDbConnection>("postgres").To<NpgsqlConnection>()
            .Bind<IConnectionPool>().To<ConnectionPool>()

            // Composition root
            .Root<IConnectionPool>("ConnectionPool");

        var composition = new Composition();
        var pool = composition.ConnectionPool;

        // Check that the pool has created 3 connections
        pool.Connections.Length.ShouldBe(3);
        pool.Connections[0].ShouldBeOfType<NpgsqlConnection>();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IDbConnection;

// Specific implementation for PostgreSQL
class NpgsqlConnection : IDbConnection;

interface IConnectionPool
{
    ImmutableArray<IDbConnection> Connections { get; }
}

class ConnectionPool([Tag("postgres")] Func<IDbConnection> connectionFactory) : IConnectionPool
{
    public ImmutableArray<IDbConnection> Connections { get; } =
    [
        // Use the factory to create distinct connection instances
        connectionFactory(),
        connectionFactory(),
        connectionFactory()
    ];
}
// }