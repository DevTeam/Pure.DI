/*
$v=true
$p=0
$d=Injections of abstractions
$h=This example demonstrates the recommended approach of using abstractions instead of implementations when injecting dependencies.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.InjectionsOfAbstractionsScenario;

using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    void DoSomething();
}

class Service(IDependency dependency) : IService
{
    public void DoSomething() { }
}

class Program(IService service)
{
    public void Run() => service.DoSomething();
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {            
        DI.Setup("Composition")
            // Binding abstractions to their implementations:
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            // Specifies to create a composition root (a property)
            // of type "Program" with the name "Root":
            .Root<Program>("Root");
        
        var composition = new Composition();

        // root = new Program(new Service(new Dependency()));
        var root = composition.Root;

        root.Run();
// }            
        composition.SaveClassDiagram();
    }
}