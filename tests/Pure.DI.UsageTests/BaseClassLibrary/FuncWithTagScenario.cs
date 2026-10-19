/*
$v=true
$p=2
$d=Func with tag
$sa=Func
$sa=Tags
$h=A tag applied to a `Func<T>` dependency carries over to the instances it creates. Here `[Tag("postgres")] Func<IDbConnection>` resolves the binding registered with the `"postgres"` tag, and each call returns a new `NpgsqlConnection`, letting the pool create as many distinct connections as it needs.
$f=>[!NOTE]
$f=>Func with tags allows you to create instances with specific tags dynamically, useful for factory patterns with multiple implementations.
$f=
$f=This is what a pool needs: the consumer chooses the implementation by tag once and then creates as many instances as it wants. The tag on the parameter decides which binding the factory creates, so the pool never sees the composition.
$f=The generated instances follow the lifetime of the tagged binding: a transient connection yields a new object per call, while a singleton would yield the same one every time and defeat the pool. Verify the expected count and type in a test, as this scenario does.
$f=>[!TIP]
$f=>Prefer a tagged `Func<T>` over injecting several tagged instances when the number of objects is decided at run time.
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