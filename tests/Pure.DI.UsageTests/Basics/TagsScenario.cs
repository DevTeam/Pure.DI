/*
$v=true
$p=6
$d=Tags
$h=This example demonstrates the most efficient way to get the root object of a composition without impacting memory consumption or performance.
$f=Sometimes it's important to take control of building a dependency graph. In this case, _tags_ help: 
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
namespace Pure.DI.UsageTests.Basics.TagsScenario;

using Shouldly;
using Xunit;

// {
internal interface IDependency { }

internal class AbcDependency : IDependency { }
        
internal class XyzDependency : IDependency { }
        
internal class Dependency : IDependency { }

internal interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }
}

internal class Service : IService
{
    public Service(
        [Tag("Abc")] IDependency dependency1,
        [Tag("Xyz")] IDependency dependency2)
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
        // FormatCode = Off
// {            
        DI.Setup("Composition")
            .Bind<IDependency>("Abc").To<AbcDependency>()
            .Bind<IDependency>("Xyz").To<XyzDependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency1.ShouldBeOfType<AbcDependency>();
        service.Dependency2.ShouldBeOfType<XyzDependency>();
// }            
        TestTools.SaveClassDiagram(composition, nameof(TagsScenario));
    }
}