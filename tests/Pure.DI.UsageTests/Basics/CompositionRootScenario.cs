/*
$v=true
$p=0
$d=Composition root
$h=This example demonstrates the most efficient way to create a composition root. There is no limit to the number of roots, but consider limiting this number. Ideally, an application would like to have a single composition root.
$f=The name of the composition root is arbitrarily chosen according to its purpose, but must be limited by C# property naming conventions. Actually, the property _Root_ looks like:
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
namespace Pure.DI.UsageTests.Basics.CompositionRootScenario;

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
        // ToString = On
        // FormatCode = On
// {            
        DI.Setup("Composition")
            .Bind<IDependency>().To<Dependency>()
                // Creates a private root that is only accessible from _Resolve_ methods:
                .Root<IDependency>()
            .Bind<IService>().To<Service>()
                // Creates a regular public root named _Root_
                .Root<IService>("Root")
            .Bind<IService>("Other").To<OtherService>()
                // Creates a public root named _OtherService_ using the _Other_ tag:
                .Root<IService>("OtherService", "Other");

        var composition = new Composition();
        var service = composition.Root;
        var otherService = composition.OtherService;
        var dependency = composition.Resolve<IDependency>();
// }            
        service.ShouldBeOfType<Service>();
        TestTools.SaveClassDiagram(composition, nameof(CompositionRootScenario));
    }
}