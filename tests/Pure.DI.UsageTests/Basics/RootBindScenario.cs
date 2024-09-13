/*
$v=true
$p=17
$d=Root binding
$h=In general, it is recommended to define one composition root for the entire application. But sometimes it is necessary to have multiple roots. To simplify the definition of composition roots, a "hybrid" API method `RootBind<T>(string rootName)` was added. It allows you to define a binding and at the same time the root of the composition. You can it in order to reduce repetitions. The registration `composition.RootBind<IDependency>().To<Dependency>()` is an equivalent to `composition.Bind<IDependency>().To<Dependency>().Root<IDependency>()`.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.RootBindScenario;

using Shouldly;
using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {            
        DI.Setup(nameof(Composition))
            .Bind().As(Lifetime.Singleton).To<Dependency>()
            .RootBind<IService>("MyRoot").To<Service>();
        // It's the same as:
        //   .Bind<IService>().To<Service>()
        //   .Root<IService>("MyRoot")

        var composition = new Composition();
        composition.MyRoot.ShouldBeOfType<Service>();
// }
        composition.SaveClassDiagram();
    }
}