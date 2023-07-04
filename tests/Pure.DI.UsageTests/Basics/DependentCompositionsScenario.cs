/*
$v=true
$p=12
$d=Dependent compositions
$h=The _Setup_ method has an additional argument _kind_, which defines the type of composition:
$h=- _CompositionKind.Public_ - will create a normal composition class, this is the default setting and can be omitted, it can also use the _DependsOn_ method to use it as a dependency in other compositions
$h=- _CompositionKind.Internal_ - the composition class will not be created, but that composition can be used to create other compositions by calling the _DependsOn_ method with its name
$h=- _CompositionKind.Global_ - the composition class will also not be created, but that composition will automatically be used to create other compositions
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
namespace Pure.DI.UsageTests.Basics.DependentCompositionsScenario;

using Pure.DI;
using UsageTests;
using Shouldly;
using Xunit;

// {
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService { }

internal class Service : IService
{
    public Service(IDependency dependency, DateTime now)
    {
    }
}

internal class Program
{
    public Program(IService service)
    {
        Service = service;
    }

    public IService Service { get; }
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
        // This setup affects all compositions created
        // and does not require the use of the "DependsOn" call to add it as a dependency
        DI.Setup("MyGlobal", CompositionKind.Global)
            .Bind<DateTime>().To(_ => DateTime.Now);
        
        // This setup does not generate code, but can be used as a dependency
        // and requires the use of the "DependsOn" call to add it as a dependency
        DI.Setup("BaseComposition", CompositionKind.Internal)
            .Bind<IDependency>().To<Dependency>();
        
        // This setup generates code and can also be used as a dependency
        DI.Setup("Composition")
            // Uses "BaseComposition" setup
            .DependsOn("BaseComposition")
            .Bind<IService>().To<Service>().Root<IService>("Root");
        
        // As in the previous case, this setup generates code and can also be used as a dependency
        DI.Setup("OtherComposition")
            // Uses "Composition" setup
            .DependsOn("Composition")
            .Root<Program>("Program");
        
        var composition = new Composition();
        var service = composition.Root;

        var otherComposition = new OtherComposition();
        service = otherComposition.Program.Service;
// }            
        service.ShouldBeOfType<Service>();
        TestTools.SaveClassDiagram(otherComposition, nameof(DependentCompositionsScenario));
    }
}