/*
$v=true
$p=5
$d=Arguments
$h=Sometimes you need to pass some state to a composition class to use it when resolving dependencies. To do this, just use the `Arg<T>(string argName)` method, specify the type of argument and its name. You can also specify a tag for each argument. You can then use them as dependencies when building the object graph. If you have multiple arguments of the same type, just use tags to distinguish them. The values of the arguments are manipulated when you create a composition class by calling its constructor. It is important to remember that only those arguments that are used in the object graph will appear in the constructor. Arguments that are not involved will not be added to the constructor arguments.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Basics.ArgumentsScenario;

using Shouldly;
using Xunit;

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
    string Name { get; }
    
    IDependency Dependency { get; }
}

class Service(
    [Tag("name")] string name,
    IDependency dependency) : IService
{
    public string Name { get; } = name;

    public IDependency Dependency { get; } = dependency;
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup("Composition")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root")
            // Some argument
            .Arg<int>("id")
            // An argument can be tagged (e.g., tag "name")
            // to be injectable by type and this tag
            .Arg<string>("serviceName", "name");

        var composition = new Composition(serviceName: "Abc", id: 123);
        var service = composition.Root;
        service.Name.ShouldBe("Abc");
        service.Dependency.Id.ShouldBe(123);
// }
        composition.SaveClassDiagram();
    }
}