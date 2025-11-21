/*
$v=true
$p=17
$d=Root binding
$h=In general, it is recommended to define one composition root for the entire application. But sometimes it is necessary to have multiple roots. To simplify the definition of composition roots, a "hybrid" API method `RootBind<T>(string rootName)` was added. It allows you to define a binding and at the same time the root of the composition. You can it in order to reduce repetitions. The registration `composition.RootBind<IDependency>().To<Dependency>()` is an equivalent to `composition.Bind<IDependency>().To<Dependency>().Root<IDependency>()`.
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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind().As(Lifetime.Singleton).To<DbConnection>()
            // RootBind allows you to define a binding and a composition root
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