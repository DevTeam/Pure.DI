/*
$v=true
$p=15
$d=Required properties or fields
$h=All properties or fields marked with the _required_ keyword automatically become injected dependencies.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Basics.RequiredPropertiesOrFieldsScenario;

using Shouldly;
using Xunit;

// {
interface IDependency { }

class Dependency : IDependency { }

interface IService
{
    string Name { get;}
    
    IDependency Dependency { get;}
}

class Service : IService
{
    public required string ServiceNameField;

    public string Name => ServiceNameField;

    public required IDependency Dependency { get; init; }
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
            .Arg<string>("name")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition(name: "My Service");
        var service = composition.Root;
        service.Dependency.ShouldBeOfType<Dependency>();
        service.Name.ShouldBe("My Service");
// }            
        TestTools.SaveClassDiagram(composition, nameof(RequiredPropertiesOrFieldsScenario));
    }
}