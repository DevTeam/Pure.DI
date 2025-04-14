/*
$v=true
$p=3
$d=Injections as required
$h=This example demonstrates using dependency injection with Pure.DI to dynamically create dependencies as needed via a factory function. The code defines a service (`Service`) that requires multiple instances of a dependency (`Dependency`). Instead of injecting pre-created instances, the service receives a `Func<IDependency>` factory delegate, allowing it to generate dependencies on demand.
$f=Key elements:
$f=- `Dependency` is bound to the `IDependency` interface, and `Service` is bound to `IService`.
$f=- The `Service` constructor accepts `Func<IDependency>`, enabling deferred creation of dependencies.
$f=- The `Service` calls the factory twice, resulting in two distinct `Dependency` instances stored in its `Dependencies` collection.
$f=
$f=This approach showcases how factories can control dependency lifetime and instance creation timing in a DI container. The Pure.DI configuration ensures the factory resolves new `IDependency` instances each time it's invoked, achieving "injections as required" functionality.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.InjectionsAsRequiredScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Generic;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve=Off
// {
        DI.Setup(nameof(Composition))
            .Bind().To<Dependency>()
            .Bind().To<Service>()

            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependencies.Count.ShouldBe(2);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    IReadOnlyList<IDependency> Dependencies { get; }
}

class Service(Func<IDependency> dependencyFactory): IService
{
    public IReadOnlyList<IDependency> Dependencies { get; } =
    [
        dependencyFactory(),
        dependencyFactory()
    ];
}
// }