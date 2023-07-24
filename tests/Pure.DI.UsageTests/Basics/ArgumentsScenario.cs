/*
$v=true
$p=5
$d=Arguments
$h=Sometimes you need to pass some state to the composition class in order to use it when resolving dependencies. Just use the `Arg` method, specify the type of the argument and the name of the argument. A tag can also be specified for each argument. After that, they can be used as dependencies when building an object graph. If you have multiple arguments of the same type, just use tags to distinguish between them.
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

class Dependency : IDependency
{
    public Dependency(int id) => Id = id;

    public int Id { get; }
}

interface IService
{
    string Name { get; }
    
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(
        [Tag("name")] string name,
        IDependency dependency)
    {
        Name = name;
        Dependency = dependency;
    }

    public string Name { get; }
    
    public IDependency Dependency { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
        // FormatCode = On
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
        TestTools.SaveClassDiagram(composition, nameof(ArgumentsScenario));
    }
}