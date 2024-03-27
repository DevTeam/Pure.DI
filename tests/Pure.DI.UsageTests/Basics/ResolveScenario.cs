/*
$v=true
$p=1
$d=Resolve methods
$h=This example shows how to resolve the composition roots using the _Resolve_ methods by _Service Locator_ approach. `Resolve` methods are generated automatically for each registered root.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.ResolveScenario;

using Shouldly;
using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

class OtherService : IService;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {    
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            .Bind<IService>("Other").To<OtherService>()
            
            // Specifies to create a private composition root
            // of type "IDependency" with the name "Dependency"
            .Root<IDependency>()
            
            // Specifies to create a private root
            // that is only accessible from _Resolve_ methods
            .Root<IService>()
            
            // Specifies to create a public root named _OtherService_
            // using the _Other_ tag
            .Root<IService>("OtherService", "Other");

        var composition = new Composition();
        var dependency = composition.Resolve<IDependency>();
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
        composition.SaveClassDiagram();
    }
}