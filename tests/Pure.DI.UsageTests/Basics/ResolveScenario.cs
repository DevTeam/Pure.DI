/*
$v=true
$p=1
$d=Resolve methods
$h=This example shows how to resolve the composition roots using the _Resolve_ methods by _Service Locator_ approach. `Resolve` methods are generated automatically for each registered root.
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

internal class OtherService : IService
{
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
            .Bind<IService>().To<Service>().Root<IService>("Service")
            .Bind<IService>("Other").To<OtherService>().Root<IService>("OtherService", "Other");

        var composition = new Composition();
        var service1 = composition.Resolve<IService>();
        var service2 = composition.Resolve(typeof(IService));
        
        // Resolve by tag
        var otherService1 = composition.Resolve<IService>("Other");
        var otherService2 = composition.Resolve(typeof(IService),"Other");
// }            
        service1.ShouldBeOfType<Service>();
        service2.ShouldBeOfType<Service>();
        otherService1.ShouldBeOfType<OtherService>();
        otherService2.ShouldBeOfType<OtherService>();
        TestTools.SaveClassDiagram(composition, nameof(ResolveScenario));
    }
}