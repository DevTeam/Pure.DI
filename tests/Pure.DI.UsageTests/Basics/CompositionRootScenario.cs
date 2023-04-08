/*
$v=true
$p=0
$d=Composition root
$h=This example demonstrates the most efficient way to obtain a composition root. The number of roots are not limited.
$f=Actually, the property _Root_ looks like:
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
namespace Pure.DI.UsageTests.Basics.CompositionRootScenario;

using Shouldly;
using Xunit;

// {
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService { }

internal class Service : IService
{
    public Service(IDependency dependency)
    {
    }
}

internal class OtherService : IService
{
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
// {            
        DI.Setup("Composition")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>("Other").To<OtherService>()
            .Bind<IService>().To<Service>()
            // The only argument is the name of the root property
            .Root<IService>("Root")
            // The first argument is the name of the root property, and the second argument is the tag               
            .Root<IService>("OtherRoot", "Other");

        var composition = new Composition();
        var service = composition.Root;
        var otherService = composition.OtherRoot;
// }            
        service.ShouldBeOfType<Service>();
        otherService.ShouldBeOfType<OtherService>();
        TestTools.SaveClassDiagram(composition, nameof(CompositionRootScenario));
    }
}