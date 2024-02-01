/*
$v=true
$p=1
$d=Composition roots
$h=This example demonstrates several ways to create a composition root. There is no limit to the number of roots, but you should consider limiting the number of roots. Ideally, an application should have a single composition root.
$f=The name of the root of a composition is arbitrarily chosen depending on its purpose, but should be restricted by the property naming conventions in C# since it is the same name as a property in the composition class. In reality, the _Root_ property has the form:
$f=```c#
$f=public IService Root
$f={
$f=  get
$f=  {
$f=    return new Service(new Dependency());
$f=  }
$f=}
$f=``` 
$f=To avoid generating _Resolve_ methods just add a comment `// Resolve = Off` before a _Setup_ method:
$f=```c#
$f=// Resolve = Off
$f=DI.Setup("Composition")            
$f=  .Bind<IDependency>().To<Dependency>()
$f=  ...
$f=```
$f=This can be done if these methods are not needed, in case only certain composition roots are used. It's not significant then, but it will help save resources during compilation.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.CompositionRootsScenario;

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
            .Bind<IService>().To<Service>()
                // Specifies to create a regular public composition root
                // of type "IService" with the name "MyRoot":
                .Root<IService>("MyRoot")
            .Bind<IService>("Other").To<OtherService>()
                // Specifies to create a regular public composition root
                // of type "IService" with the name "SomeOtherService"
                // using the "Other" tag:
                .Root<IService>("SomeOtherService", "Other")
            .Bind<IDependency>().To<Dependency>()
                // Creates a private composition root
                // that is only accessible from "Resolve()" methods:
                .Root<IDependency>();

        var composition = new Composition();
        
        // service = new Service(new Dependency());
        var service = composition.MyRoot;
        
        // someOtherService = new OtherService();
        var someOtherService = composition.SomeOtherService;
        
        var dependency = composition.Resolve<IDependency>();
// }            
        service.ShouldBeOfType<Service>();
        composition.SaveClassDiagram();
    }
}