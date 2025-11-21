/*
$v=true
$p=99
$d=Service provider with scope
$h=> [!IMPORTANT]
$h=> Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface. These roots must be registered using `Root(...)` or `RootBind()` calls.
$r=Shouldly;Microsoft.Extensions.DependencyInjection
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace Pure.DI.UsageTests.BCL.ServiceProviderWithScopeScenario;

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
        using var composition = new Composition();

        // Creates the first scope (e.g., for a web request)
        using var scope1 = composition.CreateScope();
        var session1 = scope1.ServiceProvider.GetRequiredService<ISession>();
        var config1 = composition.GetRequiredService<IConfiguration>();

        // The session must use the global configuration
        session1.Configuration.ShouldBe(config1);

        // Within the same scope, the session instance must be the same
        session1.ShouldBe(scope1.ServiceProvider.GetRequiredService<ISession>());

        // Creates the second scope
        using var scope2 = composition.CreateScope();
        var session2 = scope2.ServiceProvider.GetRequiredService<ISession>();
        var config2 = composition.GetRequiredService<IConfiguration>();

        session2.Configuration.ShouldBe(config2);
        session2.ShouldBe(scope2.ServiceProvider.GetRequiredService<ISession>());

        // Sessions in different scopes are different instances
        session1.ShouldNotBe(session2);

        // Configuration is a singleton, so it's the same instance
        config1.ShouldBe(config2);
        // }
        composition.SaveClassDiagram();
    }
}

// {
// Represents a global configuration (Singleton)
interface IConfiguration;

class Configuration : IConfiguration;

// Represents a user session (Scoped)
interface ISession : IDisposable
{
    IConfiguration Configuration { get; }
}

class Session(IConfiguration configuration) : ISession
{
    public IConfiguration Configuration { get; } = configuration;

    public void Dispose() {}
}

partial class Composition
    : IKeyedServiceProvider, IServiceScopeFactory, IServiceScope
{
    static void Setup() =>
        // The following hint overrides the name of the
        // "object Resolve(Type type)" method in "GetService",
        // which implements the "IServiceProvider" interface:
        DI.Setup()
            // The following hint overrides the name of the
            // "object Resolve(Type type)" method in "GetService",
            // which implements the "IServiceProvider" interface
            .Hint(Hint.ObjectResolveMethodName, "GetService")
            // The following hint overrides the name of the
            // "object Resolve(Type type, object tag)" method in "GetRequiredKeyedService",
            // which implements the "IKeyedServiceProvider" interface
            .Hint(Hint.ObjectResolveByTagMethodName, "GetRequiredKeyedService")
            .Bind<IConfiguration>().As(Lifetime.Singleton).To<Configuration>()
            .Bind<ISession>().As(Lifetime.Scoped).To<Session>()

            // Composition roots
            .Root<IConfiguration>()
            .Root<ISession>();

    public IServiceProvider ServiceProvider => this;

    public IServiceScope CreateScope() => new Composition(this);

    public object GetKeyedService(Type serviceType, object? serviceKey) =>
        GetRequiredKeyedService(serviceType, serviceKey);
}
// }