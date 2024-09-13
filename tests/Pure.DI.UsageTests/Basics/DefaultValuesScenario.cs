/*
$v=true
$p=15
$d=Default values
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.DefaultValuesScenario;

using Shouldly;
using Xunit;

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