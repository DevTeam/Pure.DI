/*
$v=true
$p=3
$d=PerBlock
$h=The _PreBlock_ lifetime does not guarantee that there will be a single dependency instance for each instance of the composition root (as for the _PreResolve_ lifetime), but is useful for reducing the number of instances of a type.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable UnusedParameter.Global
#pragma warning disable CA1822

namespace Pure.DI.UsageTests.Lifetimes.PerBlockScenario;

using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
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
            // Bind DatabaseConnection with PerBlock lifetime:
            // Ensures a single connection per composition root (e.g., per user request),
            // but a new one for each new root - useful for batch operations without full singleton overhead.
            .Bind().As(PerBlock).To<DatabaseConnection>()
            // Bind a tuple of two connections as Singleton:
            // This shares the same connection globally, simulating a cached or shared resource.
            .Bind().As(Singleton).To<(IDatabaseConnection conn3, IDatabaseConnection conn4)>()

            // Composition root - represents the main service entry point.
            .Root<OrderRepository>("Repository");

        var composition = new Composition();

        // Simulate the first user request or batch operation
        var repository1 = composition.Repository;
        repository1.ProcessOrder("ORD-2025-54546");

        // Check that within one repository (one block), connections are shared for consistency
        repository1.PrimaryConnection.ShouldBe(repository1.SecondaryConnection);
        repository1.OtherConnection.ShouldBe(repository1.FallbackConnection);
        repository1.PrimaryConnection.ShouldBe(repository1.OtherConnection);

        // Simulate the second user request or batch - should have a new PerBlock connection
        var repository2 = composition.Repository;
        repository2.PrimaryConnection.ShouldNotBe(repository1.PrimaryConnection);
// }
        composition.SaveClassDiagram();
    }
}

// {
// Interface for database connection - in a real world, this could handle SQL queries
interface IDatabaseConnection;

// Implementation of database connection - transient-like but controlled by lifetime
class DatabaseConnection : IDatabaseConnection;

// Repository for handling orders, injecting multiple connections for demonstration
// In real-world, this could process orders in a batch, sharing connection within the batch
class OrderRepository(
    IDatabaseConnection primaryConnection,
    IDatabaseConnection secondaryConnection,
    (IDatabaseConnection otherConnection, IDatabaseConnection fallbackConnection) additionalConnections)
{
    // Public properties for connections - in practice, these would be private and used in methods
    public IDatabaseConnection PrimaryConnection { get; } = primaryConnection;

    public IDatabaseConnection SecondaryConnection { get; } = secondaryConnection;

    public IDatabaseConnection OtherConnection { get; } = additionalConnections.otherConnection;

    public IDatabaseConnection FallbackConnection { get; } = additionalConnections.fallbackConnection;

    // Example real-world method: Process an order using the shared connection
    public void ProcessOrder(string orderId)
    {
        // Use PrimaryConnection to query database, e.g.,
        // "SELECT * FROM Orders WHERE Id = @orderId"
    }
}
// }