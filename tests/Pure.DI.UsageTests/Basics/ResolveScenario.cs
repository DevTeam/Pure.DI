/*
$v=true
$p=1
$d=Resolve methods
$h=This example shows how to resolve the composition roots using the _Resolve_ methods. 
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
namespace Pure.DI.UsageTests.Basics.ResolveScenario;

using Shouldly;
using Xunit;

// {
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService { }

internal class Service : IService
{
    public Service(IDependency dependency)
    {
    }
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
            .Bind<IService>().To<Service>();

        var composition = new Composition();
        var service1 = composition.Resolve<IService>();
        var service2 = composition.Resolve(typeof(IService));
// }            
        service1.ShouldBeOfType<Service>();
        service2.ShouldBeOfType<Service>();
        TestTools.SaveClassDiagram(composition, nameof(ResolveScenario));
    }
}