/*
$v=true
$p=27
$d=Root binding
$sa=Composition roots
$h=`RootBind<T>(rootName)` is a shorthand that declares a binding and a composition root in one call, for the case where a type is both the entry point and something other services depend on.
$h=It removes the repetition in `Bind<IDependency>().To<Dependency>().Root<IDependency>()`, which matters in compositions that expose several entry points.
$f=The call returns a binding builder, so `.To<TImpl>()`, `.As(Lifetime.Singleton)` and tags are chained exactly as on `Bind<T>()`.
$f=The root name is a member name on the composition, so it must be a valid identifier and unique among roots (`DIE004`, `DIE005`).
$f=The root exposes the contract, not the implementation: keep `RootBind<IOrderService>` so the caller sees the abstraction and can be given a decorator later.
$f=Prefer the explicit `Bind(...).Root(...)` pair when the binding and the root deliberately differ in type or tag; `RootBind` is for the identical case only.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.RootBindScenario;

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
            .Bind().As(Lifetime.Singleton).To<DbConnection>()
            // RootBind lets you define a binding and a composition root
            // simultaneously. This is useful for creating public entry points
            // for your application components while keeping the configuration concise.
            .RootBind<IOrderService>("OrderService").To<OrderService>();

        // The line above is functionally equivalent to:
        //  .Bind<IOrderService>().To<OrderService>()
        //  .Root<IOrderService>("OrderService")

        var composition = new Composition();
        var orderService = composition.OrderService;
        orderService.ShouldBeOfType<OrderService>();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IDbConnection;

class DbConnection : IDbConnection;

interface IOrderService;

class OrderService(IDbConnection connection) : IOrderService;
// }