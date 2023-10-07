/*
$v=true
$p=14
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
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Basics.DependentCompositionsScenario;

using Pure.DI;
using UsageTests;
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

class Program
{
    public Program(IService service) =>
        Service = service;

    public IService Service { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {    
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
        otherComposition.SaveClassDiagram();
    }
}