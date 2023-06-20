/*
$v=true
$p=5
$d=Arguments
$h=Sometimes you need to pass some state to the composition class in order to use it when resolving dependencies. Just use the `Arg` method, specify the type of the argument and the name of the argument. A tag can also be specified for each argument. After that, they can be used as dependencies when building an object graph. If you have multiple arguments of the same type, just use tags to distinguish between them.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
namespace Pure.DI.UsageTests.Basics.ArgumentsScenario;

using Shouldly;
using Xunit;

// {
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService
{
    string Id { get; }

    string Name { get; }
}

internal class Service : IService
{
    public Service(
        string id,
        [Tag("name")] string name,
        IDependency dependency)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }
    
    public string Name { get; }
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
            // The argument can be tagged to be available for injection by type and that tag
            .Arg<string>("serviceName", "name")
            // An untagged argument
            .Arg<string>("id");

        var composition = new Composition(serviceName: "Abc", id: "123");
        var service = composition.Root;
        service.Name.ShouldBe("Abc");
        service.Id.ShouldBe("123");
// }
        TestTools.SaveClassDiagram(composition, nameof(ArgumentsScenario));
    }
}