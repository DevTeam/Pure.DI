﻿/*
$v=true
$p=12
$d=Partial class
$h=A partial class can contain setup code.
$f=The partial class is also useful for specifying access modifiers to the generated class.
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Basics.PartialClassScenario;

using Shouldly;
using Xunit;

// {
interface IDependency { }

class Dependency : IDependency { }

interface IService { }

class Service : IService
{
    public Service(IDependency dependency) { }
}

// The partial class is also useful for specifying access modifiers to the generated class
partial class Composition
{
    // This method will not be called in runtime
    private void Setup() =>
// }        
        // ToString = On
        // FormatCode = On
// {        
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");
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
        var composition = new Composition();
        var service = composition.Root;
// }            
        service.ShouldBeOfType<Service>();
        TestTools.SaveClassDiagram(composition, nameof(PartialClassScenario));
    }
}