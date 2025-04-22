/*
$v=true
$p=3
$d=Injections on demand with arguments
$h=This example illustrates dependency injection with parameterized factory functions using Pure.DI, where dependencies are created with runtime-provided arguments. The scenario features a service that generates dependencies with specific IDs passed during instantiation.
$f=Key components:
$f=- `Dependency` class accepts an int id constructor argument, stored in its `Id` property.
$f=- `Service` receives `Func<int, IDependency>` delegate, enabling creation of dependencies with dynamic values.
$f=- `Service` creates two dependencies using the factory – one with ID `33`, another with ID `99`.
$f=
$f=Delayed dependency instantiation:
$f=- Injection of dependencies requiring runtime parameters
$f=- Creation of distinct instances with different configurations
$f=- Type-safe resolution of dependencies with constructor arguments
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.InjectionOnDemandWithArgumentsScenario;

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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind().To<Dependency>()
            .Bind().To<Service>()

            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        var dependencies = service.Dependencies;
        dependencies.Count.ShouldBe(2);
        dependencies[0].Id.ShouldBe(33);
        dependencies[1].Id.ShouldBe(99);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency
{
    int Id { get; }
}

class Dependency(int id) : IDependency
{
    public int Id { get; } = id;
}

interface IService
{
    IReadOnlyList<IDependency> Dependencies { get; }
}

class Service(Func<int, IDependency> dependencyFactoryWithArgs): IService
{
    public IReadOnlyList<IDependency> Dependencies { get; } =
    [
        dependencyFactoryWithArgs(33),
        dependencyFactoryWithArgs(99)
    ];
}
// }