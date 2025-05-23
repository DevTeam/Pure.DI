﻿/*
$v=true
$p=99
$d=Service provider
$h=The `// ObjectResolveMethodName = GetService` hint overriding the `object Resolve(Type type)` method name in `GetService()`, allowing the `IServiceProvider` interface to be implemented in a partial class.
$h=> [!IMPORTANT]
$h=> Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface. These roots must be registered using `Root(...)` or `RootBind()` calls.
$h=
$h=This example demonstrates how to implement a custom `IServiceProvider` using a partial class, utilizing a specific hint to override the default `Resolve()` method name:
$f=Important Notes:
$f=- Hint Overriding: The `ObjectResolveMethodName = GetService` hint overrides the default object `Resolve(Type type)` method name to implement `IServiceProvider` interface
$f=- Roots: Only roots can be resolved. Use `Root(...)` or `RootBind()` calls for registration
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
        var service = serviceProvider.GetRequiredService<IService>();
        var dependency = serviceProvider.GetRequiredService<IDependency>();
        service.Dependency.ShouldBe(dependency);
// }
        serviceProvider.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition : IServiceProvider
{
    static void Setup() =>
        DI.Setup()
            // The following hint overrides the name of the
            // "object Resolve(Type type)" method in "GetService",
            // which implements the "IServiceProvider" interface
            .Hint(Hint.ObjectResolveMethodName, "GetService")
            .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<IDependency>()
            .Root<IService>();
}
// }