/*
$v=true
$p=2
$d=Factory
$h=Demonstrates how to use factories for manual creation and initialization when constructor injection alone is not enough.
$h=Use factory bindings for custom setup, external APIs, or controlled object state during creation.
$f=There are scenarios where manual control over the creation process is required, such as
$f=- When additional initialization logic is needed
$f=- When complex construction steps are required
$f=- When specific object states need to be set during creation
$f=
$f=>[!IMPORTANT]
$f=>The method `Inject()` cannot be used outside of the binding setup.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.Basics.FactoryScenario;

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
            .Bind<IDatabaseService>().To<DatabaseService>(ctx => {
                // Some logic for creating an instance.
                // For example, we need to manually initialize the connection.
                ctx.Inject(out DatabaseService service);
                service.Connect();
                return service;
            })
            .Bind<IUserRegistry>().To<UserRegistry>()

            // Composition root
            .Root<IUserRegistry>("Registry");

        var composition = new Composition();
        var registry = composition.Registry;
        registry.Database.IsConnected.ShouldBeTrue();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDatabaseService
{
    bool IsConnected { get; }
}

class DatabaseService : IDatabaseService
{
    public bool IsConnected { get; private set; }

    // Simulates a connection establishment that must be called explicitly
    public void Connect() => IsConnected = true;
}

interface IUserRegistry
{
    IDatabaseService Database { get; }
}

class UserRegistry(IDatabaseService database) : IUserRegistry
{
    public IDatabaseService Database { get; } = database;
}
// }
