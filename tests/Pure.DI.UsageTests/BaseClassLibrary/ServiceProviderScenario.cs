/*
$v=true
$p=18
$d=Service provider
$sa=Service collection
$sa=Service provider with scope
$h=The `// ObjectResolveMethodName = GetService` hint overriding the `object Resolve(Type type)` method name in `GetService()`, allowing the `IServiceProvider` interface to be implemented in a partial class.
$h=>[!IMPORTANT]
$h=>Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface. These roots must be registered using `Root(...)` or `RootBind()` calls.
$h=
$h=This example shows how to implement a custom `IServiceProvider` using a partial class, utilizing a specific hint to override the default `Resolve()` method name:
$f=Important Notes:
$f=- Hint Overriding: The `ObjectResolveMethodName = GetService` hint overrides the default object `Resolve(Type type)` method name to implement `IServiceProvider` interface
$f=- Roots: Only roots can be resolved. Use `Root(...)` or `RootBind()` calls for registration
$f=A hand-written `IServiceProvider` is the smallest integration: no container is created, and `GetRequiredService` runs against the generated composition. The resolution by `Type` is a lookup in generated code, so an unregistered type is reported as a missing service at run time.
$f=Because resolution goes through roots, a type that is injected internally but not declared as a root is not reachable through `IServiceProvider` even though the graph uses it. Declare a root for every service that outside code may ask for.
$f=>[!TIP]
$f=>Renaming `Resolve` through hints means `Hint.Resolve = Off` cannot be used in the same composition; keep resolve methods enabled wherever a `ServiceProvider`-shaped API is exposed.
$r=Shouldly;Microsoft.Extensions.DependencyInjection
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace Pure.DI.UsageTests.BCL.ServiceProviderScenario;

using Microsoft.Extensions.DependencyInjection;
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
// {
        var serviceProvider = new Composition();
        var orderService = serviceProvider.GetRequiredService<IOrderService>();
        var logger = serviceProvider.GetRequiredService<ILogger>();

        // Check that the singleton instance is correctly injected
        orderService.Logger.ShouldBe(logger);
// }
        serviceProvider.SaveClassDiagram();
    }
}

// {
// Represents a dependency, e.g., a logging service
interface ILogger;

class ConsoleLogger : ILogger;

// Represents a service that depends on ILogger
interface IOrderService
{
    ILogger Logger { get; }
}

class OrderService(ILogger logger) : IOrderService
{
    public ILogger Logger { get; } = logger;
}

partial class Composition : IServiceProvider
{
    static void Setup() =>
        DI.Setup()
            // The following hint overrides the name of the
            // "object Resolve(Type type)" method in "GetService",
            // which implements the "IServiceProvider" interface
            .Hint(Hint.ObjectResolveMethodName, "GetService")
            .Bind<ILogger>().As(Lifetime.Singleton).To<ConsoleLogger>()
            .Bind<IOrderService>().To<OrderService>()

            // Roots are required for resolution via IServiceProvider
            .Root<ILogger>()
            .Root<IOrderService>();
}
// }