/*
$v=true
$p=5
$d=Arguments
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
    string Name { get; }
}

internal class Service : IService
{
    public Service(string name, IDependency dependency)
    {
        Name = name;
    }

    public string Name { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
// {            
        DI.Setup("Composition")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            .Arg<string>("serviceName")
            .Root<IService>("Root");

        var composition = new Composition("Abc");
        var service = composition.Root;
        service.Name.ShouldBe("Abc");
// }
        TestTools.SaveClassDiagram(composition, nameof(ArgumentsScenario));
    }
}