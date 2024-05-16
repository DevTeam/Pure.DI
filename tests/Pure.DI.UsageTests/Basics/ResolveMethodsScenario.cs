﻿/*
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
namespace Pure.DI.UsageTests.Basics.ResolveMethodsScenario;

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
            .Bind<IService>("My Tag").To<OtherService>()
            
            // Specifies to create a private root
            // that is only accessible from _Resolve_ methods
            .Root<IService>()
            
            // Specifies to create a public root named _OtherService_
            // using the "My Tag" tag
            .Root<IService>("OtherService", "My Tag");

        var composition = new Composition();

        // The next 3 lines of code do the same thing:
        var service1 = composition.Resolve<IService>();
        var service2 = composition.Resolve(typeof(IService));
        var service3 = composition.Resolve(typeof(IService), null);
        
        // Resolve by "My Tag" tag
        // The next 3 lines of code do the same thing too:
        var otherService1 = composition.Resolve<IService>("My Tag");
        var otherService2 = composition.Resolve(typeof(IService),"My Tag");
        var otherService3 = composition.OtherService; // Gets the composition through the public root
// }            
        service1.ShouldBeOfType<Service>();
        service2.ShouldBeOfType<Service>();
        service3.ShouldBeOfType<Service>();
        otherService1.ShouldBeOfType<OtherService>();
        otherService2.ShouldBeOfType<OtherService>();
        otherService3.ShouldBeOfType<OtherService>();
        composition.SaveClassDiagram();
    }
}