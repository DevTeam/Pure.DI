/*
$v=true
$p=8
$d=Multi-contract bindings
$h=An unlimited number of contracts can be attached to one implementation. Including their combinations with various tags.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedVariable
// ReSharper disable ArrangeTypeModifiers
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.MultiContractBindingsScenario;

using Xunit;

// {
interface IDependency;

interface IAdvancedDependency;

class Dependency : IDependency, IAdvancedDependency;

interface IService;

class Service(
    IDependency dependency,
    IAdvancedDependency advancedDependency)
    : IService;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup(nameof(Composition))
            .Bind<IDependency, IAdvancedDependency>().To<Dependency>()
            // .Bind<IDependency>().Bind<IAdvancedDependency>().To<Dependency>()
            // is also allowed
            .Bind<IService>().To<Service>()
            
            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
// }            
        composition.SaveClassDiagram();
    }
}