/*
$v=true
$p=1
$d=Composition roots simplified
$h=You can use `RootBind<T>()` method in order to reduce repetitions.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.CompositionRootsSimplifiedScenario;

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
            // Specifies to create a regular public composition root
            // of type "IService" with the name "MyRoot" and
            // it's the equivalent of statements
            // .Bind<IService>().To<Service>().Root<IService>("MyRoot")
            .RootBind<IService>("MyRoot").To<Service>()
            
            // Specifies to create a private composition root
            // that is only accessible from "Resolve()" methods and
            // it's the equivalent of statements
            // .Bind<IService>("Other").To<OtherService>().Root<IService>("MyRoot")
            .RootBind<IService>(tags: "Other").To<OtherService>()
            
            .Bind().To<Dependency>();

        var composition = new Composition();
        
        // service = new Service(new Dependency());
        var service = composition.MyRoot;
        
        // someOtherService = new OtherService();
        var someOtherService = composition.Resolve<IService>("Other");
// }            
        service.ShouldBeOfType<Service>();
        composition.SaveClassDiagram();
    }
}