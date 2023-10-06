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
namespace Pure.DI.UsageTests.Basics.MultiContractBindingsScenario;

using Xunit;

// {
interface IDependency { }

interface IAdvancedDependency { }

class Dependency : IDependency, IAdvancedDependency { }

interface IService { }

class Service : IService
{
    public Service(
        IDependency dependency,
        IAdvancedDependency advancedDependency) { }
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
            .Bind<IDependency>().Bind<IAdvancedDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
// }            
        composition.SaveClassDiagram();
    }
}