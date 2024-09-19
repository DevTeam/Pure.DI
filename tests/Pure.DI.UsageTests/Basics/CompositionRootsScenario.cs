/*
$v=true
$p=1
$d=Composition roots
$h=This example demonstrates several ways to create a composition root.
$h=> [!TIP]
$h=> There is no limit to the number of roots, but you should consider limiting the number of roots. Ideally, an application should have a single composition root.
$h=
$h=If you use classic DI containers, the composition is created dynamically every time you call a method similar to `T Resolve<T>()` or `object GetService(Type type)`. The root of the composition there is simply the root type of the composition of objects in memory T or Type type. There can be as many of these as you like. In the case of Pure.DI, the number of composition roots is limited because for each composition root a separate property or method is created at compile time. Therefore, each root is defined explicitly by calling the `Root(string rootName)` method.
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
            .Bind<IService>("Other").To<OtherService>()
            .Bind<IDependency>().To<Dependency>()

            // Specifies to create a regular public composition root
            // of type "IService" with the name "MyService"
            .Root<IService>("MyService")

            // Specifies to create a private composition root
            // that is only accessible from "Resolve()" methods
            .Root<IDependency>()

            // Specifies to create a regular public composition root
            // of type "IService" with the name "MyOtherService"
            // using the "Other" tag
            .Root<IService>("MyOtherService", "Other");

        var composition = new Composition();

        // service = new Service(new Dependency());
        var service = composition.MyService;

        // someOtherService = new OtherService();
        var someOtherService = composition.MyOtherService;

        // All and only the roots of the composition
        // can be obtained by Resolve method
        var dependency = composition.Resolve<IDependency>();
        
        // including tagged ones
        var tagged = composition.Resolve<IService>("Other");
// }
        service.ShouldBeOfType<Service>();
        tagged.ShouldBeOfType<OtherService>();
        composition.SaveClassDiagram();
    }
}