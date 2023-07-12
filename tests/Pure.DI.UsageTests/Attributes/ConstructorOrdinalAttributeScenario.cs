/*
$v=true
$p=0
$d=Constructor ordinal attribute
$h=When applied to any constructor in a type, automatic injection constructor selection is disabled. The selection will only focus on constructors marked with this attribute, in the appropriate order from smallest value to largest.
$f=The attribute `Ordinal` is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Attributes.ConstructorOrdinalAttributeScenario;

using Shouldly;
using Xunit;

// {
interface IDependency { }

class Dependency : IDependency { }

interface IService { }

class Service : IService
{
    private readonly string _name;

    [Ordinal(1)]
    public Service(IDependency dependency) =>
        _name = "with dependency";
    
    [Ordinal(0)]
    internal Service(string name) => _name = name;
    
    public Service() => _name = "default";

    public override string ToString() => _name;
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
            .Arg<string>("serviceName")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition("Xyz");
        var service = composition.Root;
        service.ToString().ShouldBe("Xyz");
// }            
        TestTools.SaveClassDiagram(composition, nameof(ConstructorOrdinalAttributeScenario));
    }
}