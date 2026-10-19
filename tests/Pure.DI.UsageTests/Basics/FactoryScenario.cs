/*
$v=true
$t=QuickStart
$t=Basics
$p=5
$p=QuickStart:8
$d=Factory
$sa=Simplified factory
$sa=Injection on demand
$sa=Overrides
$h=Constructor injection covers most cases, but sometimes an instance needs extra work before it is ready to use - like the `Connect()` call here that opens a database connection.
$h=A factory binding `To<T>(ctx => ...)` puts that creation logic under your control: call `ctx.Inject(out var dependency)` to have the container provide dependencies, run any setup code, then return the finished instance.
$f=Fewer surprises come from keeping a factory to what a constructor cannot express: connection setup, registering a callback, setting a property that has no constructor parameter.
$f=The factory runs every time the binding is resolved, so a transient factory means a connection opened per call; give it a lifetime (`As(Lifetime.Singleton)`) when the setup is expensive and meant to happen once.
$f=>[!IMPORTANT]
$f=>The method `Inject()` cannot be used outside of the binding setup.
$r=Shouldly
$f=`ctx.Inject` is synchronous only: async initialization is not supported in a factory (`DIE020`), so move it behind an explicit async method on the service.
$f=Prefer a constructor or a [simplified factory](simplified-factory.md) when the dependencies are all it needs; a factory that only calls `ctx.Inject` for each parameter duplicates a constructor by hand.
$f=See also: [Simplified factory](simplified-factory.md), [Injection on demand](injection-on-demand.md).
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
