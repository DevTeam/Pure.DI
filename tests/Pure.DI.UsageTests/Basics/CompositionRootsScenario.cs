/*
$v=true
$p=0
$d=Composition roots
$h=This example demonstrates several ways to create the roots of a composition. There is no limit to the number of roots, but you should consider limiting the number of roots. Ideally, an application should have a single composition root.
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
namespace Pure.DI.UsageTests.Basics.CompositionRootsScenario;

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

class OtherService : IService
{
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup("Composition")
            .Bind<IService>().To<Service>()
                // Creates a regular public root named "Root"
                .Root<IService>("Root")
            .Bind<IService>("Other").To<OtherService>()
                // Creates a public root named "OtherService"
                // using the "Other" tag:
                .Root<IService>("OtherService", "Other")
            .Bind<IDependency>().To<Dependency>()
                // Creates a private root
                // that is only accessible from "Resolve()" methods:
                .Root<IDependency>();

        var composition = new Composition();
        var service = composition.Root;
        var otherService = composition.OtherService;
        var dependency = composition.Resolve<IDependency>();
// }            
        service.ShouldBeOfType<Service>();
        composition.SaveClassDiagram();
    }
}