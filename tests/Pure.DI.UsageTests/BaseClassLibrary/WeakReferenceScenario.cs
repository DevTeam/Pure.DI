/*
$v=true
$p=6
$d=Weak Reference 
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageTests.BCL.WeakReferenceScenario;

using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(WeakReference<IDependency> dependency) : IService
{
    public IDependency? Dependency => 
        dependency.TryGetTarget(out var value)
            ? value
            : default;
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup("Composition")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
// }            
        composition.SaveClassDiagram();
    }
}