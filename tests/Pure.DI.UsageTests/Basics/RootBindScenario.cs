/*
$v=true
$p=27
$d=Root binding
$sa=Composition roots
$h=In general, it is recommended to define one composition root for the entire application. But Sometimes you need to have multiple roots. To simplify the definition of composition roots, a "hybrid" API method `RootBind<T>(string rootName)` was added. It lets you define a binding and at the same time the root of the composition. You can it in order to reduce repetitions. The registration `composition.RootBind<IDependency>().To<Dependency>()` is an equivalent to `composition.Bind<IDependency>().To<Dependency>().Root<IDependency>()`.
$f=>[!NOTE]
$f=>`RootBind` reduces boilerplate when you need both a binding and a root for the same type.
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