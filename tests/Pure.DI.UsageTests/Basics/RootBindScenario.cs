/*
$v=true
$p=17
$d=Root binding
$h=You might want to register some services as roots. You can use `RootBind<T>()` method in order to reduce repetitions. The registration `composition.RootBind<IDependency>().To<Dependency>()` is an equivalent to `composition.Bind<IDependency>().To<Dependency>().Root<IDependency>()`.
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

class Dependency: IDependency;

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
            .RootBind<IService>("Root").To<Service>();

        var composition = new Composition();
        composition.Root.ShouldBeOfType<Service>();
// }
        composition.SaveClassDiagram();
    }
}