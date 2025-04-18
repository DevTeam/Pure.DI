/*
$v=true
$p=15
$d=Default values
$h=This example demonstrates how to use default values in dependency injection when explicit injection is not possible.
$f=The key points are:
$f=- Default constructor arguments can be used for simple values
$f=- The DI container will use these defaults if no explicit bindings are provided
$f=
$f=This example illustrates how to handle default values in a dependency injection scenario:
$f=- **Constructor Default Argument**: The `Service` class has a constructor with a default value for the name parameter. If no value is provided, “My Service” will be used.
$f=- **Required Property with Default**: The Dependency property is marked as required but has a default instantiation. This ensures that:
$f=  - The property must be set
$f=  - If no explicit injection occurs, a default value will be used
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.DefaultValuesScenario;

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
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()

            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency.ShouldBeOfType<Dependency>();
        service.Name.ShouldBe("My Service");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    string Name { get; }

    IDependency Dependency { get; }
}

// If injection cannot be performed explicitly,
// the default value will be used
class Service(string name = "My Service") : IService
{
    public string Name { get; } = name;

    public required IDependency Dependency { get; init; } = new Dependency();
}
// }