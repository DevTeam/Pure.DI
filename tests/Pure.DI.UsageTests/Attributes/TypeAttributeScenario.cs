/*
$v=true
$p=4
$d=Type Attribute
$h=The injection type can be defined manually using the `Type` attribute. This attribute explicitly overrides an injected type, otherwise it would be determined automatically based on the type of the constructor/method, property, or field parameter.
$f=This attribute is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageTests.Attributes.TypeAttributeScenario;

using Shouldly;
using Xunit;

// {
internal interface IDependency { }

internal class AbcDependency : IDependency { }

internal class XyzDependency : IDependency { }

internal interface IService
{
    IDependency Dependency1 { get; }
    
    IDependency Dependency2 { get; }
}

internal class Service : IService
{
    public Service(
        [Type(typeof(AbcDependency))] IDependency dependency1,
        [Type(typeof(XyzDependency))] IDependency dependency2)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
    }
    
    public IDependency Dependency1 { get; }
    
    public IDependency Dependency2 { get; }
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
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency1.ShouldBeOfType<AbcDependency>();
        service.Dependency2.ShouldBeOfType<XyzDependency>();
// }            
        TestTools.SaveClassDiagram(composition, nameof(TypeAttributeScenario));
    }
}