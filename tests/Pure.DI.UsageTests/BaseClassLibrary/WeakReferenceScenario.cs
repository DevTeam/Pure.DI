/*
$v=true
$p=6
$d=Weak Reference 
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable
namespace Pure.DI.UsageTests.BCL.WeakReferenceScenario;

using Xunit;

// {
interface IDependency { }

class Dependency : IDependency { }

interface IService { }

class Service : IService
{
    private readonly WeakReference<IDependency> _dependency;

    public Service(WeakReference<IDependency> dependency) => 
        _dependency = dependency;

    public IDependency? Dependency => 
        _dependency.TryGetTarget(out var value)
            ? value
            : default;
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
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
// }            
        TestTools.SaveClassDiagram(new Composition(), nameof(WeakReferenceScenario));
    }
}